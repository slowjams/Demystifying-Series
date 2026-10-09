## Demystifying Kubernetes


```bash
kubectl run color-api --image=slowjams/color-api:1.0.0 # --dry-run=client -o yaml

kubectl get pod nginx-pod -o yaml

kubectl run nginx --image=nginx:1.27.0

kubectl run -it alpine --image=alpine:3.20 sh
apk --update add curl

kubectl get pods
kubectl logs nginx --container=nginx
kubectl expose pod nginx --type=NodePort --port=80
```

```bash
kubectl get service

# NAME         TYPE        CLUSTER-IP      EXTERNAL-IP   PORT(S)        AGE
# kubernetes   ClusterIP   10.96.0.1       <none>        443/TCP        20d
# nginx        NodePort    10.107.19.247   <none>        80:31668/TCP   10m
```


## Installing Tools


```bash
minikube start --kubernetes-version=v1.32.0   #  this cluster runs as a docker container    

curl -LO "https://dl.k8s.io/release/v1.36.2/bin/linux/amd64/kubectl"  # install kubectl

curl -LO https://github.com/kubernetes/minikube/releases/latest/download/minikube-linux-amd64    # create a mini K8s cluster 

kubectl config get-contexts
# * docker-desktop  <---------current, next to switch to minikube
# minikube

kubectl config use-context minikube  # updates your local Kubernetes configuration (kubeconfig) so kubectl can correctly connect to your Minikube cluster.      
```
note that you can enable Kubernetes in Docker Desktop, Docker Desktop creates a Kubernetes cluster for you, but we don't want to use this cluster


==================================================================================

## Minikube

```
Windows
   │
Docker Desktop
   │
Docker Engine
   │
   └── minikube container
          │
          ├── kubernetes  <-----------------------no more dockerd, kubernetes DOES NOT need dockerd
          │
          └── containerd
                │
                └── runc
                     │
                     └── your Pod
```

Inside the Minikube container, Kubernetes creates the containers for pods, so it is like "containers inside container", that's why you cannot see the Pod container in Docker Desktop

==================================================================================


`kubelet`: To register the node with the control plane and to ensure that the containers described in the Pod specifications assigned to its node are running and healthy.

The Kubelet is the "node agent." **Its first job** is to announce its existence and health to the control plane when a node joins Kubernetes, its kubelet essentially tells the control plane:

```
              CONTROL PLANE
           ┌─────────────────┐
           │   API Server    │
           └─────────────────┘
                    ▲
                    │ "I'm here. I'm node worker-1, and I'm healthy." Then kubelet periodically reports information such as node's available CPI, Memory
                    │  and status of Pods running in this worker node back to the API server
                    │
        ┌──────────────────────┐
        │      WORKER NODE     │
        │                      │
        │      kubelet         │
        │         │            │
        |        ...           |
        └──────────────────────┘
```


**Its main, ongoing job** is to watch the API server for Pods scheduled on its node and then manage their lifecycle (start, stop, health check) by interacting with the local container runtime:

```
              CONTROL PLANE
           ┌─────────────────┐
           │   API Server    │
           └────────┬────────┘
                    │
                    │ "Run Pod A"
                    ▼
        ┌──────────────────────┐
        │      WORKER NODE     │
        │                      │
        │      kubelet         │
        │         │            │
        │         ▼            │
        │  Container Runtime   │
        │         │            │
        │         ▼            │
        │    Container/Pod     │
        └──────────────────────┘
```
=======================================================================================================


```
Kubernetes API
│
├── core group
│   └── v1
│       ├── Pod
│       ├── Service
│       ├── ConfigMap
│       └── Secret
│
├── apps group
│   └── v1
│       ├── Deployment
│       ├── ReplicaSet
│       ├── StatefulSet
│       └── DaemonSet
│
├── batch group
│   └── v1
│       ├── Job
│       └── CronJob
│
└── networking.k8s.io
    └── v1
        ├── Ingress
        └── NetworkPolicy
```



## Declarative vs Imperative Commands

```yml
# nginx-pod.yaml
apiVersion: v1

kind: Pod

metadata:
  name: nginx-pod
  labels:
    app: nginx

spec:
  containers:
    - name: nginx-container
      image: nginx:1.27.0
      ports:
        - containerPort: 80
```

**Imperative**:
```bash
kubectl create -f nginx-pod.yaml

## change the image to nginx:1.27.0-alpine and then do

kubectl replace -f nginx-pod.yaml
# Output: Error, because there are some other things like volume are not specified in the yml file and volume is created by k8s on the first time 
# and k8s will manage all non-specific setting by itself; and when you do `kubectl replace`, the yamnl file doesn't have setting like volume
# that's why the error appear
```

**Declarative**:
```bash
kubectl create -f nginx-pod.yaml

# `apply` make K8s to only apply the diff change which is the image version change
kubectl apply -f nginx-pod.yaml
```
=======================================================================================================


## `last-applied-configuration`

`last-applied-configuration` come up when you use `kubectl apply`, it performs a three-way merge between the previous state (from the annotation), the live state in the cluster, and the new state from the file to accurately calculate what needs to be changed.

```bash
kubectl apply -f nginx-pod.yaml
kubectl get pod nginx-pod -o yaml  # output the yaml below
```
```yml
apiVersion: v1
kind: Pod
metadata:
  annotations:
    kubectl.kubernetes.io/last-applied-configuration: |   # <----------------------------------------------
      {"apiVersion":"v1","kind":"Pod","metadata":{"annotations":{},"labels":{"app":"nginx"},"name":"nginx-pod","namespace":"default"},"spec":{"containers":[{"image":"nginx:1.27.0-alpine","name":"nginx-container","ports":[{"containerPort":80}]}]}}
  creationTimestamp: "2026-10-04T12:35:01Z"
  labels:
    app: nginx
  name: nginx-pod
  namespace: default
  resourceVersion: "55913"
  uid: 11f3eeb5-7b4d-4b2d-95b7-55ee47b34cea
spec:
  ...
```
let's see how K8s use last-applied-configuration:

`Scenario One`: intentional deletion

Initial YAML:
```yml
spec:
  containers:
    - name: nginx
      image: nginx:1.25
      env:
        - name: DEBUG
          value: "true"
```
after you `apply` above yaml, last-applied-configuration section is created, now you change your YAML to remove `env`
```yml
spec:
  containers:
    - name: nginx
      image: nginx:1.27
```
and do `kubectl apply` again, now K8s can concludes that: **"env was previously managed by me, but the user removed it from the desired configuration. So I will remove `env` from the live object"**

`Scenario Two`: prevent unintentional deletion
Suppose you never had env in your YAML:
```yml
spec:
  containers:
    - name: nginx
      image: nginx:1.25
```
but another person manually added `DEBUG: true` in the live object, and if you apply the modifed yaml again, now K8s concludes that: **"`env` wasn't part of the configuration I previously applied, so I shouldn't assume the user wants me to delete it."**

=======================================================================================================

## ReplicaSet

```yml
# nginx-rs.yaml
apiVersion: apps/v1
kind: ReplicaSet
metadata:
  name: nginx-replicaset
spec:
  replicas: 3
  selector:
    matchLabels:
      app: nginx  # <-------------this label is used to identify the pods that belong to this ReplicaSet
  template:
    metadata:
      labels:
        app: nginx  #<------------the labels in the template section MUST match the labels in the selector section.
                    # this ensures that the Pods created by the ReplicaSet will be managed by that same ReplicaSet.
                    # if the template's labels didn't match the selector, the ReplicaSet would create Pods that it couldn't see, 
                    # leading to an infinite loop of Pod creation. The Kubernetes API server enforces this match.
    spec:
      containers:
      - name: nginx
        image: nginx:1.27.0
        ports:
        - containerPort: 80
```
says you do:
1. `kubectl apply -f nginx-rs.yaml`
2. change the yaml's image to a different version `nginx:1.27.0-alpine`
3. `kubectl apply -f nginx-rs.yaml` again

then **the 3 Pods still contain old image versions**, you have to delete all 3 exsiting pods so that new pods will have the new image version, that is one of reasons why we need a higher abstraction **Deployment** to manage the shortcomings of ReplicaSet

says you do:
1. `kubectl apply -f nginx-pod.yaml` to manually create a Pod first
2. `kubectl apply -f nginx-rs.yaml`
the replicaSet will only create two Pods, the exsiting pod will count as part of the replicaSet

says you do:
1. `kubectl apply -f nginx-rs.yaml`
2. `kubectl apply -f nginx-pod.yaml` to try manually creating a Pod first
the new manually created pod will count as part of the replicaSet, and replicaSet will delete one pod, it could be the manual Pod, or it could be one of the existing Pods from the replicaSet, depending on Kubernetes' Pod-selection logic.

So now you see A ReplicaSet can maintain a stable number of Pods for a single version of an application, but it offers **no automated strategy for transitioning from one version to another**

This is the correct answer. This is a critical validation rule. If the template's labels didn't match the selector, the ReplicaSet would create Pods that it couldn't see, leading to an infinite loop of Pod creation. The Kubernetes API server enforces this match.

================================================================================


## Deployment

```yml
# nginx-depl.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: nginx-deployment
  labels:
    app: nginx
  # annotations:  # <-------------says we add it for the second kubectl apply -f
    # "kubernetes.io/change-cause": "update nginx to tag 1.27.0-alpine"

  # $ kubectl rollout history deployment nginx-deployment
  # deployment.apps/nginx-deployment
  # REVISION  CHANGE-CAUSE
  # 1         <none>
  # 2         update nginx to tag 1.27.0-alpine

spec:
  replicas: 3
  selector:
    matchLabels:
      app: nginx
  template:
    metadata:
      labels:
        app: nginx 
    spec:
      containers:
      - name: nginx
        image: nginx:1.27.0
        ports:
        - containerPort: 80
  # strategy:   
  #   type: RollingUpdate  <-------- RollingUpdate is the default
  #   rollingUpdate:
  #     maxSurge: 25%        <-------2 extra Pods are allowed, if replicas: 5, 25% of 5 = 1.25, rounded up to 2.
  #     maxUnavailable: 25%  <-------1 Pod can be unavailable, if replicas: 5, 25% of 5 = 1.25, rounded down to 1.
```

```RollingUpdate

        Old RS          New RS
        v1              v2

Start    3               0
         ↓               ↓
         2               1
         ↓               ↓
         1               2
         ↓               ↓
Finish   0               3
```

```
Deployment
    │
    │ manages
    ▼
ReplicaSets  <---------> Revision, each revision corresponds to a ReplicaSet that represents a particular version of the Deployment's Pod template
    │
    │ manage
    ▼
Pods
```

```bash
kubectl apply -f nginx-depl.yaml  # first run
# change nginx:1.27.0 to nginx:1.27.0-alpine
kubectl apply -f nginx-depl.yaml  # second run

$ kubectl get deploy
# NAME               READY   UP-TO-DATE   AVAILABLE   AGE
# nginx-deployment      3/3     3            3           3h2m

$ kubectl describe deploy nginx-deployment
# Name:                   nginx-deployment
# Namespace:              default
# CreationTimestamp:      Tue, 06 Oct 2026 20:04:51 +1100
# Labels:                 app=nginx
# Annotations:            deployment.kubernetes.io/revision: 3
# Selector:               app=nginx
# Replicas:               5 desired | 5 updated | 5 total | 5 available | 0 unavailable
# StrategyType:           RollingUpdate
# MinReadySeconds:        0
# RollingUpdateStrategy:  25% max unavailable, 25% max surge
# Pod Template:
# ...

kubectl get rs  # two rs, hash such as 8bdf8f46b is the hash value of the template content of nginx-depl.yaml
# NAME                          DESIRED   CURRENT   READY   AGE
# nginx-deployment-6d8469f8db   0         0         0       95m
# nginx-deployment-8bdf8f46b    3         3         3       167m

$ kubectl describe rs nginx-deployment-8bdf8f46b
# Name:           nginx-deployment-8bdf8f46b
# Namespace:      default
# Selector:       app=nginx,pod-template-hash=8bdf8f46b
# Labels:         app=nginx
#                 pod-template-hash=8bdf8f46b
# ...

kubectl rollout history deployment nginx-deployment  # kubectl rollout history deployment/nginx-deployment  (same, just different syntax)
# deployment.apps/nginx-deployment
# REVISION  CHANGE-CAUSE
# 1         <none>   <------------------Initial apply, nginx:1.27.0
# 2         <none>   <------------------Change image,  nginx:1.27.0-alpine

# Initial apply
#     ↓
# Revision 1
#     ↓
# Change image
#     ↓
# Revision 2

kubectl rollout undo deployment nginx-deployment # <------------rollout undo create a new revision, not "point" back to previous revision
kubectl rollout history deployment nginx-deployment 
# deployment.apps/nginx-deployment
# REVISION  CHANGE-CAUSE
# 2         <none>
# 3         <none>   <----------------note that Revision 1 is deleted, beause Revision 1's ReplicaSet was deleted as part of cleanup probably because
                                    # Revision 3 uses same template (nginx:1.27.0) as Revision 1, so K8s think Revision 1 is no longer needed
                                    # but udemy tutor said Revision becomes Revision 3 as part of revision history

kubectl annotate deployment nginx-deployment "kubernetes.io/change-cause"="update nginx to tag 1.27.0-alpine"  # adds the annotation to the Deployment object
# deployment.apps/nginx-deployment                                                                             # 
# REVISION  CHANGE-CAUSE
# 2         <none>
# 3         update nginx to tag 1.27.0-alpine

kubectl describe deploy nginx-deployment
# Name:                   nginx-deployment
# Namespace:              default
# CreationTimestamp:      Tue, 06 Oct 2026 20:04:51 +1100
# Labels:                 app=nginx
# Annotations:            deployment.kubernetes.io/revision: 3  <-----------A Deployment can be associated with the current/latest revision
#                         kubernetes.io/change-cause: update nginx to tag 1.27.0-alpine  <-------------
#  ...

# replicaset for Revision 3, K8s Deployment controller propagate the annotation change to the ReplicaSet representing the current Deployment revision
kubectl describe rs nginx-deployment-8bdf8f46b  
# Name:           nginx-deployment-8bdf8f46b
# Namespace:      default
# Selector:       app=nginx,pod-template-hash=8bdf8f46b
# Labels:         app=nginx
#                 pod-template-hash=8bdf8f46b
# Annotations:    deployment.kubernetes.io/desired-replicas: x
#                 deployment.kubernetes.io/max-replicas: x
#                 deployment.kubernetes.io/revision: 3
#                 kubernetes.io/change-cause: update nginx to tag 1.27.0-alpine   <-------------------------------
#  ...
```

Revision is the version number attached to a ReplicaSet history and ReplicaSet as the physical implementation of a revision, Revision is introduced becuase it use ordered version number for each change to the Deployment's Pod template, making rollout history easier to track than using ReplicaSet names (non-number) directly.

note that `kubectl apply -f nginx-depl.yaml` will only create revision/replicaSets for Pod template changes, if you just change e.g Deployment's metadata, there will be NO new revision/replicaSets.

```bash
# kubectl get pods  <----------5 pods running
kubectl scale deploy nginx-deployment --replicas=3  # change live Kubernetes object, no roll out cahnge
# kubectl get pods  <----------3 pods running

kubectl diff -f nginx-depl.yaml
# -  generation: 8
# +  generation: 9
#  spec:
#    progressDeadlineSeconds: 600
# -  replicas: 3
# +  replicas: 5

kubectl apply -f nginx-depl.yaml
kubectl get pods  # <----------5 pods running

#-------------------------------------------------V quickly restarted all pods
kubectl scale deploy nginx-deployment --replicas=0
kubectl scale deploy nginx-deployment --replicas=5
#-------------------------------------------------Ʌ this probabably the only case that imperative command is better than declarative command
```
note that `kubectl diff -f nginx-depl.yaml` asks: **"What changes would happen to the live Kubernetes objects if I ran `kubectl apply -f nginx-depl.yaml` right now?"**


## Failed Rollout

```bash
kubectl apply -f nginx-depl.yaml  <----------5 pods running
# then you update nginx-depl.yaml with in invalid image version in template
kubectl apply -f nginx-depl.yaml

kubectl get pod
# NAME                                READY   STATUS             RESTARTS       AGE
# nginx-deployment-55f9d85f7c-2b6xs   1/1     Running            1 (4m8s ago)   22h
# nginx-deployment-55f9d85f7c-c85bs   1/1     Running            2 (4m8s ago)   2d21h
# nginx-deployment-55f9d85f7c-l2nqm   1/1     Running            2 (4m8s ago)   2d21h
# nginx-deployment-55f9d85f7c-qfkmr   1/1     Running            2 (4m8s ago)   2d21h    // one existing Pod is deleted due to rollout strategy
# nginx-deployment-8ff9bfd5f-b6w7v    0/1     ImagePullBackOff   0              21h
# nginx-deployment-8ff9bfd5f-j7m2g    0/1     ImagePullBackOff   0              21h
# nginx-deployment-8ff9bfd5f-lxq7x    0/1     ImagePullBackOff   0              21h

kubectl rollout undo deployment nginx-deployment #<----------create a new Pod who joins the PREVIOUS working ReplicaSet, it is NOT like a new ReplicaSet  
                                                 # is created and 5 different new pods created and join the new ReplicaSet

kubectl get pods                                                          
# NAME                                READY   STATUS    RESTARTS      AGE
# nginx-deployment-55f9d85f7c-2b6xs   1/1     Running   1 (80m ago)   23h
# nginx-deployment-55f9d85f7c-8smsm   1/1     Running   0             14s   <---------------new pod
# nginx-deployment-55f9d85f7c-c85bs   1/1     Running   2 (80m ago)   2d22h
# nginx-deployment-55f9d85f7c-l2nqm   1/1     Running   2 (80m ago)   2d22h
# nginx-deployment-55f9d85f7c-qfkmr   1/1     Running   2 (80m ago)   2d22h

kubectl rollout history deployment nginx-deployment
# REVISION  CHANGE-CAUSE
# 3         <none>
# 4         update nginx to tag 1.27.0-alpine
# 6         update nginx to tag 1.27.1-alpine <--------- at this moment, this ReplicaSet contains 5 pods (e.g 2b6xs ...)
# 7         update nginx to tag 1.27.1-alpine <--------- revision 6 and revision 7 map to the same ReplicaSet which has existing 4 Pods, and one new Pod (8smsm)
```
```yml
# default strategy
replicas: 5
strategy:
  type: RollingUpdate
  rollingUpdate:
    maxSurge: 25%         # <-----------25% of 5, rounded up, 2
    maxUnavailable: 25%   # <-----------25% of 5, rounded down, 1
```
This means Kubernetes can temporarily run up to **7** Pods total, while allowing the number of available Pods to drop to **4**.
You might expect Kubernetes to create a fifth healthy Pod from the old ReplicaSet. However, the Deployment controller is not designed to automatically roll back in response to a failed rollout.

With `maxUnavailable: 0` and `maxSurge: 1`, Kubernetes repeats this process until all old Pods have been replaced:
```
| Stage                  | Old Pods | New Pods | Available Pods       |
|------------------------|----------|----------|----------------------|
| Initial                | 5        | 0        | 5                    |
| Create first new Pod   | 5        | 1        | 6, once Ready        | <-------`maxSurge: 1` makes Available Pods can be 6
| Remove one old Pod     | 4        | 1        | 5                    |
| Create second new Pod  | 4        | 2        | 6, once Ready        |
| Remove one old Pod     | 3        | 2        | 5                    |
| Continue...            | ...      | ...      | ...                  |
| Completed              | 0        | 5        | 5                    |
```