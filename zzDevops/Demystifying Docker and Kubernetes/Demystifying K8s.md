## Demystifying Kubernetes


```bash
# slowjams/color-api:1.1.0


kubectl get pod nginx-pod -o yaml

kubectl run nginx --image=nginx:1.27.0

kubectl run -it alpine --image=alpine:3.20 sh
apk --update add curl

kubectl get pods
kubectl logs nginx --container=nginx
kubectl expose pod nginx --type=NodePort --port=80
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
        - containerPort: 80  # <-------------only for metadata documentation, it doesn't make Nginx listen on port 80 so that tools such as 
                             # dashboards, monitoring systems etc. can use this information to understand what ports the container declares
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

A ReplicaSet ensures that the correct number of Pods exist; **it doesn't update existing Pods when its Pod template changes** so you will see **the 3 Pods still contain old image versions**, you have to delete all 3 exsiting pods so that new pods will have the new image version, that is one of reasons why we need a higher abstraction **Deployment** to manage the shortcomings of ReplicaSet

say you do:
1. `kubectl apply -f nginx-pod.yaml` to manually create a Pod first
2. `kubectl apply -f nginx-rs.yaml`
the replicaSet will only create two Pods, the exsiting pod will count as part of the replicaSet

say you do:
1. `kubectl apply -f nginx-rs.yaml`
2. `kubectl apply -f nginx-pod.yaml` to try manually creating a Pod first
the new manually created pod will count as part of the replicaSet, and replicaSet will delete one pod, it could be the manual Pod, or it could be one of the existing Pods from the replicaSet, depending on Kubernetes' Pod-selection logic.

So now you see a ReplicaSet can maintain a stable number of Pods for a single version of an application, but it offers **no automated strategy for transitioning from one version to another**


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

# <-----------you might ask why K8s doesn't create another new Pod to match 5 healthy Pods requirement, it is because of default maxUnavailable

kubectl rollout history deployment nginx-deployment
# REVISION  CHANGE-CAUSE
# ...
# 5         update nginx to tag 1.27.0-alpine <----at this moment, this ReplicaSet contains 5 healthy pods (e.g 2b6xs ...)
# 6         update nginx to tag 1.27.1-alpine <--- because of invalid image verion, at this moment, this ReplicaSet has 4 healthy pods, 3 unhealthy pods


kubectl rollout undo deployment nginx-deployment #<----------create a new Pod who joins the PREVIOUS working ReplicaSet, it is NOT like a new ReplicaSet  
                                                 # is created and 5 different new pods created and join the new ReplicaSet

kubectl rollout history deployment nginx-deployment
# REVISION  CHANGE-CAUSE
# ...
# 6         update nginx to tag 1.27.1-alpine <--- because of invalid image verion, at this moment, this ReplicaSet has 4 healthy pods, 3 unhealthy pods
# 7         update nginx to tag 1.27.1-alpine <--- back to 5 health pods, and one of them is the a new Pod (8smsm)

# <-----------note that there is no Revision 5 anymore, because of Revison 7 and Revision 5 have same template, so K8s change Revison 5 to Revision 7

kubectl rollout history deployment nginx-deployment --revision=7 -o yaml
# apiVersion: apps/v1
# kind: ReplicaSet
# metadata:
#   annotations:
#     deployment.kubernetes.io/desired-replicas: "5"
#     deployment.kubernetes.io/max-replicas: "7"
#     deployment.kubernetes.io/revision: "7"
#     deployment.kubernetes.io/revision-history: 5  <-------------------------Revision 7 has revision history of Revision 5

kubectl get pods                                                          
# NAME                                READY   STATUS    RESTARTS      AGE
# nginx-deployment-55f9d85f7c-2b6xs   1/1     Running   1 (80m ago)   23h
# nginx-deployment-55f9d85f7c-8smsm   1/1     Running   0             14s   <---------------new pod
# nginx-deployment-55f9d85f7c-c85bs   1/1     Running   2 (80m ago)   2d22h
# nginx-deployment-55f9d85f7c-l2nqm   1/1     Running   2 (80m ago)   2d22h
# nginx-deployment-55f9d85f7c-qfkmr   1/1     Running   2 (80m ago)   2d22h

kubectl get rs
# NAME                          DESIRED   CURRENT   READY   AGE
# nginx-deployment-55f9d85f7c   5         5         5       3d22h
# nginx-deployment-6d8469f8db   0         0         0       4d2h
# nginx-deployment-8bdf8f46b    0         0         0       4d3h
# nginx-deployment-8ff9bfd5f    0         0         0       46h

# <----K8s doesn't immediately delete ReplicaSets with zero Pods because it keeps old ReplicaSets to support Deployment rollbacks
#      K8s can scale up the previous ReplicaSet and restore its Pod template, rather than having to reconstruct the old version from scratch.
#      e.g. you can do:
kubectl rollout undo deployment nginx-deployment --to-revision=5
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

=======================================================================================================


# Services

* `ClusterIP` : Service gets its ClusterIP. A Service selects backend Pods using labels, and K8s networking routes requests to eligible Pods, regardless of which node they run on
ClusterIP is the default and most secure option. It is intended for internal communication between microservices (e.g., a backend API talking to a database) where external access is not required.

* `NodePort`: the Service does not get assigned a Node IP address. Instead, Kubernetes exposes the Service on a static specific port on every node's IP address
NodePort is primarily for development, testing where you need quick external access ot an app without setting up a complex Load Balancer or Ingress controller

`192.168.1.10:30080` means:

- 192.168.1.10 = the Node's IP address.
- 30080 = the `NodePort`, which is the port Kubernetes exposes on that Node to let external traffic reach the Service

* `ExternalName`: create a CNAME record in the cluster's internal DNS. When a Pod looks up local hostname e.g. `my-db`, the cluster DNS returns outside world e.g. `db.legacy-system.com`, and if this external address changes to e.g. `db.system.com`, you only have to update one K8s's object


## ClusterIP

=====================

```yml
# color-api-depl.yaml
... # deploy 5 pods

# traffic-generator.yaml
apiVersion: v1
kind: Pod
metadata:
  name: traffic-generator
  labels:
    app: traffic-generator
spec:
  containers:
    - name: traffic-generator
      image: slowjams/traffic-gen:1.0.0  # a shell program that loop curl of given ip address which is arg
      # args:
      #   - '10.244.0.11/api'   # <-----------this is a private IP address of a color-api-depl pod in the cluster, it can break when the pod is restarted  
      #                                        and gets a new IP address, so it is better to use the service name instead of the pod IP address
      #   - '0.5'
      # args:
      #   - '10.96.89.195/api'   # <-----------this is a IP address of ClusterIP service, it is better to use  `- 'color-api-clusterip/api'` here
      #   - '0.5'                # because this IP can still changed when the service is deleted and recreated   
      args:
        - 'color-api-clusterip/api'   # <-----------this is a IP address of ClusterIP service, it is better to use the service DNS name than the Cluster IP
        - '0.5'
```

```yml
# color-api-clusterip.yaml
apiVersion: v1
kind: Service
metadata:
  name: color-api-clusterip
  labels:
    app: color-api
spec:
  type: ClusterIP   #<-----------not really need this one because ClusterIP is the default type for a service, but it is good to be explicit
  selector:
    app: color-api  # <-----------this is a label selector that matches the labels of pods created by the color-api-deployment
                    # service doesn't care how Pods are created, even though you manually create a Pod that has `app: color-api` label, service will include it
                    # says you manually create another pod with same label, then the service wil show 6 Endpoints (5 from deployment + 1 manually created)
  ports:
  - port: 80         # <-------------the port that the Service listens on
    targetPort: 80   #t <------------forwards traffic to eligible Pods' port 
```
```bash
kubectl get svc
# NAME                  TYPE        CLUSTER-IP     EXTERNAL-IP   PORT(S)   AGE
# color-api-clusterip   ClusterIP   10.96.89.195   <none>        80/TCP    22m

kubectl describe svc color-api-clusterip
# Name:                     color-api-clusterip
# Selector:                 app=color-api
# Type:                     ClusterIP
# IP:                       10.96.89.195
# Port:                     <unset>  80/TCP
# TargetPort:               80/TCP
# Endpoints:                10.244.0.10:80,10.244.0.8:80,10.244.0.9:80 + 2 more...  # <----------these are 5 private ip for 5 pods in color-api-depl deployment
                                                                                    # and if you delete and apply the deployment, endpoints automatically 
                                                                                    # updated to map the newly created Pods' private IPs
```
```bash
kubectl get pods
# NAME                                    READY   STATUS    RESTARTS   AGE
# color-api-deployment-5ddf49c68c-7lzlj   1/1     Running   0          87m
# color-api-deployment-5ddf49c68c-cltdx   1/1     Running   0          45m
# color-api-deployment-5ddf49c68c-hkcd6   1/1     Running   0          87m
# color-api-deployment-5ddf49c68c-jslz4   1/1     Running   0          87m
# color-api-deployment-5ddf49c68c-zrbps   1/1     Running   0          87m
# traffic-generator                       1/1     Running   0          110s

kubectl logs -f traffic-generator    
# Sending requests to 10.96.89.195/api every 0.5 seconds.
# [2026-10-13 13:42:59] Color: blue, Hostname: color-api-deployment-5ddf49c68c-cltdx  # <-------------now you see Service also acts as Load Balancer
# [2026-10-13 13:42:59] Color: blue, Hostname: color-api-deployment-5ddf49c68c-zrbps
# [2026-10-13 13:43:00] Color: blue, Hostname: color-api-deployment-5ddf49c68c-zrbps
# [2026-10-13 13:43:00] Color: blue, Hostname: color-api-deployment-5ddf49c68c-hkcd6
# [2026-10-13 13:43:01] Color: blue, Hostname: color-api-deployment-5ddf49c68c-7lzlj
# [2026-10-13 13:43:01] Color: blue, Hostname: color-api-deployment-5ddf49c68c-zrbps
# [2026-10-13 13:43:02] Color: blue, Hostname: color-api-deployment-5ddf49c68c-7lzlj 
```
note that **`ClusterIP` uses **label selector** that matches the labels of pods**, it doesn't care how Pods are created, even though you manually create a Pod that has same e.g. `app: color-api` label, service will track/include it.  Says you manually create another pod with same label, then the service wil show 6 Endpoints (5 from deployment + 1 manually created). Also note that ClusterIP Service does not choose a node. It routes traffic to a matching Pod, regardless of which node that Pod runs on

## NodePort

NodePort is suitable for local development/testing

```yml
apiVersion: v1
kind: Service
metadata:
  name: color-api-nodeport
  labels:
    app: color-api
spec:
  type: NodePort
  # externalTrafficPolicy: Local  # Only local Pods  
  selector:
    app: color-api 
  ports:
  - port: 80         
    targetPort: 80
    nodePort: 30007
```
we have to test in real Linux (https://killercoda.com/playgrounds/scenario/kubernetes) not WSL (WSL uses a mapping which is like a workround):

```bash
kubectl get pod -o wide

# NAME                                    READY   STATUS    RESTARTS   AGE     IP              NODE           NOMINATED NODE   READINESS GATES
# color-api-deployment-67bc4d4976-6plnr   1/1     Running   0          5m31s   192.168.1.228   node01         <none>           <none>
# color-api-deployment-67bc4d4976-hp2st   1/1     Running   0          5m31s   192.168.0.151   controlplane   <none>           <none>
# color-api-deployment-67bc4d4976-lwg6n   1/1     Running   0          5m31s   192.168.1.90    node01         <none>           <none>
# color-api-deployment-67bc4d4976-nxch4   1/1     Running   0          5m31s   192.168.0.6     controlplane   <none>           <none>
# color-api-deployment-67bc4d4976-wl8p4   1/1     Running   0          5m31s   192.168.1.35    node01         <none>           <none>

kubectl get node -o wide

# NAME           STATUS   ROLES           AGE   VERSION   INTERNAL-IP   EXTERNAL-IP   OS-IMAGE             KERNEL-VERSION              CONTAINER-RUNTIME
# controlplane   Ready    control-plane   10d   v1.37.1   172.30.1.2    <none>        Ubuntu 24.04.5 LTS   6.8.0-146-generic (amd64)   containerd://2.2.1
# node01         Ready    <none>          10d   v1.37.1   172.30.2.2    <none>        Ubuntu 24.04.5 LTS   6.8.0-146-generic (amd64)   containerd://2.2.1

curl 172.30.2.2:30007
curl 172.30.1.2:30007
```
note that K8s configures networking rules so that a NodePort on any node can forward traffic to a backend Pod running on another node, to enforce reuested Node does not forward the request to Pods on different Node, use `externalTrafficPolicy`


## ExternalName

Suppose your application runs inside K8s, but your database is hosted outside the cluster.

```yml
apiVersion: v1
kind: Service
metadata:
  name: database
spec:
  type: ExternalName
  externalName: prod-db.example.com   # <--------should NOT include the http:// or https:// prefix, just the domain name, because it asks for DNS CNAME record
```

K8s DNS returns a CNAME record pointing to the external hostname. The application then connects directly to the resolved destination, so that you app can use
`database:5432` instead of `prod-db.example.com:5432`


==================================================================================


## Label and Selector

Label:

```yml
apiVersion: v1
kind: Pod
metadata:
  name: color-backend
  labels:
    app: color-api
    environment: local
    tier: backend
spec:
  containers:
  - name: color-backend
    image: slowjams/color-api:1.1.0
    ports:
      - containerPort: 80
---
apiVersion: v1
kind: Pod
metadata:
  name: color-frontend
  labels:
    app: color-api
    environment: local
    tier: frontend
spec:
  containers:
  - name: color-nginx
    image: nginx:1.27.0
    ports:
      - containerPort: 80
```
```bash
kubectl apply -f .

# uppcasecase L for displaying label
kubectl get pods -L app,tier, nonExist       
# NAME             READY   STATUS    RESTARTS   AGE   APP         TIER       NONEXIST
# color-backend    1/1     Running   0          20m   color-api   backend
# color-frontend   1/1     Running   0          20m   color-api   frontend

# lowercase L for filtering label
kubectl get pods -l tier=frontend  
# NAME             READY   STATUS    RESTARTS   AGE
# color-frontend   1/1     Running   0          20m

kubectl get pod -l tier=frontend -l app=color-api   # <---------------`OR` for mulitple `-l`
# NAME             READY   STATUS    RESTARTS   AGE
# color-backend    1/1     Running   0          24m
# color-frontend   1/1     Running   0          24m

kubectl get pod -l tier=frontend,app=color-api      # <---------------`AND` for single `-l` with `,` for multiple "and" condition
# NAME             READY   STATUS    RESTARTS   AGE
# color-frontend   1/1     Running   0          25m

kubectl get pod -l 'tier in (frontend)'
kubectl get pod -l 'tier notin (frontend)'
kubectl get pod -l "tier=backend, status!=unstable"
```

Selector:
```yml
...
selector:          # <--------------selector logic is AND
  app: web-store
  env: production
```
This selector match pods with labels: `app: web-store`, `env: production` and `tier: frontend`, first two needs to be matched exactly because selector logic is `AND`, pods can have extra other labels such as tier: frontend ... 


===============================================================================================


## Namespace

Namespaces create virtual clusters. They solve naming collisions (you can reuse names like database or frontend across environments) and act as a boundary e.g for `dev`, `stg`, `prd` etc for governance (limiting CPU/RAM usage or restricting who can delete resources), which means Namespaces enable logical isolation of resources within the same physical cluster. Initial namespaces: `default`, `kube-system`, `kube-public`, and `kube-node-lease`

```yml
apiVersion: v1
kind: Namespace
metadata:
  name: dev   # <-----------------------
```

```bash
kubectl get ns
# NAME              STATUS   AGE
# default           Active   38d
# dev               Active   8s
# kube-node-lease   Active   38d
# kube-public       Active   38d
# kube-system       Active   38d
```
```yml
apiVersion: v1
kind: Pod
metadata:
  name: color-api
  namespace: dev
...
```
```bash
kubectl get pod -n dev
kubectl get pod --all-namespaces  ## kubectl get pod -A    <------------ `-A` is shorthand for --all-namespaces

kubectl config set-context --current --namespace=dev
```
note that if you delete a namespace, all the resources such as pods will be deleted too, so **be extremely careful about deleting namespaces!!! be extremely careful about deleting namespaces!!! be extremely careful about deleting namespaces!!!**, you should use security role based access control to limit who can delete namespaces, thats also why `kubectl delete -f .` work becuase if it delete ns first, it will delete the pod too, while reververly `kubectl apply -f .`
can sometimes throw an error because k8s tries to create a Pod under the ns which has not been created yet


Cross-Namespace Communication: `<serviceName>.<namespace>.svc.cluster.local`, ".svc.cluster.local" is to distinguish real subdomain or K8s's subdomain by K8s DNS resolver 
```yml
apiVersion: v1
kind: Pod
metadata:
  name: traffic-generator
  labels:
    app: traffic-generator
spec:
  containers:
    - name: traffic-generator
      image: slowjams/traffic-gen:1.0.0
      args:
        # <serviceName>.<namespace>.svc.cluster.local
        - 'color-api-svc.dev.svc.cluster.local/api'  
        - '1'                      
```
```yml
apiVersion: v1
kind: Pod
metadata:
  name: color-api
  namespace: dev  # <-----------------------
  labels:
    app: color-api
spec:
  containers:
  - name: color-api
    image: slowjams/color-api:1.1.0
    ports:
      - containerPort: 80
---
apiVersion: v1
kind: Service
metadata:
  name: color-api-svc
  namespace: dev   # <-----------------------
spec:
  type: ClusterIP
  selector:
    app: color-api
  ports:
    - port: 80
      targetPort: 80
```

===============================================================================================


## Resource Quotas

Resource Quotas: policy applied at the **Namespace** level that defines hard limits on the number of resources (such as CPU, memory, and object counts) that can be consumed by all objects within that **namespace**.

```yml
apiVersion: v1
kind: Namespace
metadata:
  name: dev
---
apiVersion: v1
kind: ResourceQuota
metadata:
  name: dev-quota
  namespace: dev      #<--------------------ResourceQuota is applied to this namespace
spec:
  hard:               # <-------------------hard is the set of desired hard limits for each named resource
    requests.cpu: "1"   
    requests.memory: "1Gi"
    limits.cpu: "2"
    limits.memory: "2Gi"
```
Pdd:
```yml
apiVersion: v1
kind: Pod
metadata:
  name: color-api
  namespace: dev    
  labels:
    app: color-api
spec:
  containers:
  - name: color-api
    image: slowjams/color-api:1.1.0  
    ports:
      - containerPort: 80
    resources:
      requests:
        cpu: "200m"      # 0.2 CPU cores
        memory: "256Mi"
      limits:
        cpu: "500m"
        memory: "512Mi"
```
Physically, the application in a Pod may use only 32 MiB of RAM. K8s **does NOT** automatically fill the remaining 256 MiB with unused memory or reserve a dedicated 200 MiB block of physical RAM for the container. Even if Pod A uses just 32 MiB, K8s normally considers 256 MiB accounted for **when evaluating** whether another Pod fits.

let's see a scenerio that go beyond the limits:
```bash
# say there is a deployment of 4 pods, each pod consumes cpu: "200m" memory: "256Mi", so if there is a 5th pod, then k8s cannot schedule it
kubectl describe resourcequota dev-quota -n dev

# Name:            dev-quota
# Namespace:       dev
# Resource         Used  Hard
# --------         ----  ----
# limits.cpu       2     2
# limits.memory    2Gi   2Gi
# requests.cpu     800m  1
# requests.memory  1Gi   1Gi

# now you change the image version and rollout an update, and ther is only 3 pods, not 4, but we didn't change the replica number, should expect 4 pods, why 3?
kubectl get pods -n dev
# NAME                         READY   STATUS    RESTARTS   AGE
# color-api-57867d8bb9-wl6kx   1/1     Running   0          8s
# color-api-c5447cc5b-pp88j    1/1     Running   0          30m
# color-api-c5447cc5b-pptpk    1/1     Running   0          30m

# because of RollingUpdate strategy, k8s needs to create a new pod first then bring down an old pod, but it couldn't crete a new pod becuase limit has hit
kubectl get rs -n dev
# NAME                   DESIRED   CURRENT   READY   AGE
# color-api-57867d8bb9   3         1         1       4m33s
# color-api-c5447cc5b    2         2         2       34m

kubectl describe rs color-api-57867d8bb9 -n dev
# ...
# Events:
#   Type     Reason            Age               From                   Message
#   ----     ------            ----              ----                   -------
  Warning  FailedCreate      6m13s               replicaset-controller  Error creating: pods "color-api-57867d8bb9-h2w8q" is forbidden: exceeded quota: ...
# ...

# you can add more capacity to the dev-quota and apply the change and then do:
kubectl rollout restart deploy color-api-depl -n dev   # to skip the exponential backoff retry which takes long time 

# note that kubectl rollout restart triggers a new Deployment rollout. It does not restart the existing ReplicaSet color-api-57867d8bb9, that is why you 
# will see a new replicaset being created
kubectl get rs -n dev
# NAME                   DESIRED   CURRENT   READY   AGE
# color-api-57867d8bb9   0         0         0       18m
# color-api-6949bd6bf8   4         4         4       4s
# color-api-c5447cc5b    0         0         0       48m
```