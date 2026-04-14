## Demystifying AWS-Solution Architect

AWS operates 24 **Regions** around the world, each region is composed of at least two, usually three **Availability Zones** (AZ). Each AZ is composed of one or more physically separated data centers. 

AWS Australia **Sydney** Region `ap-southeast-2` (ap-southeast-1 is Singapore) has 3 AZs: `ap-southeast-2a`, `ap-southeast-2b` and `ap-southeast-2c` and consist of 12 data centres:

SYD51: 1 William Dean Street, Eastern Creek NSW 2766
SYD52: 42A Bluett Drive, Smeaton Grange NSW 2567
SYD:   146 Turner Rd, Smeaton Grange NSW 2567
SYD:   148 Turner Rd, Smeaton Grange NSW 2567
SYD:   Bluett Drive, Smeaton Grange NSW 2567
SYD1:  47 Bourke Rd, Alexandria NSW 2015
SYD:   1 William Dean Street, Eastern Creek NSW 2766
SYD4:  400 Harris St, Ultimo NSW 2007 (Global Switch colocation)
SYD6:  5 Broadcast Way, Artarmon NSW 2064 (Iseek-KDC colocation) 
SYD5 : 6 Bellevue Circuit, Pemulwuy NSW 2145  (Fujitsu WSDC colocation) 
SYD61: 4 Eden Park Drive, Macquarie Park NSW 2113 (NextDC S1 colocation)
SYD7:  4 Figtree Drive, Sydney Olympic Park NSW (Fujitsu SDC colocation)


## VPC

                                        
   House                      VPC          
┌───────────┐           ┌───────────┐   
│  sublet1  │           │  subnet1  │   
├───────────┤           ├───────────┤   
│  sublet2  │  analogy  │  subnet2  │   
├───────────┤  ──────►  ├───────────┤   
│  sublet3  │           │  subnet3  │   
├───────────┤           ├───────────┤   
│   ...     │           │    ...    │   
└───────────┘           └───────────┘   

   
                                                 
                    VPC                        
┌───────────────────────────────────────────────┐
│                                               │
│     ┌─────────────────────────────────────┐   │
│     │ ┌─────────────┐ ┌─────────────┐     │   │
│ AZ1 │ │public subnet│ │privat subnet│ ... │   │
│     │ └─────────────┘ └─────────────┘     │   │
│     └─────────────────────────────────────┘   │
│     ┌─────────────────────────────────────┐   │
│     │ ┌─────────────┐ ┌─────────────┐     │   │
│ AZ2 │ │public subnet│ │privat subnet│ ... │   │
│     │ └─────────────┘ └─────────────┘     │   │
│     └─────────────────────────────────────┘   │
│     ┌─────────────────────────────────────┐   │
│     │ ┌─────────────┐ ┌─────────────┐     │   │
│ ... │ │public subnet│ │privat subnet│ ... │   │
│     │ └─────────────┘ └─────────────┘     │   │
│     └─────────────────────────────────────┘   │
│                                               │
└───────────────────────────────────────────────┘
                                        

A VPC is a logically isolated portion within a region. A VPC can contain multiple AZs and each AZ is physically isolated. 
A VPC spans across all of the AZs in the Region

```bash
# private address ranges
10.0.0.0     -   10.255.255.255  (CIDR block: 10.0.0.0/8)
172.16.0.0   -   172.31.255.255  (CIDR block: 172.16.0.0/12)
192.168.0.0  -   192.168.255.255 (CIDR block: 192.168.0.0/16)

169.254.0.0  -   169.254.255.255 (CIDR block: 169.254.0.0/16)  link-local addresses fall within the range of 
```

```bash
# default VPC
172.31.0.0/16  # <-------------------------------------------------------------------------default VPC is not recommended to use
# AWS allows you to choose a CIDR block for your VPC in the range of /16 to /28

# you can create a custom VPC with CIDR block 10.16.0.0/16

# default ap-southeast-2 subnets    last two octets         each network/subnet's host range        each subnet maps to an AZ
172.31.0.0/20                     0000|0000,00000000       start 172.31.0.0  end 172.31.15.255          ap-southeast-2a
172.31.16.0/20                    0001|0000,00000000       start 172.31.16.0 end 172.31.31.255          ap-southeast-2b
172.31.32.0/20                    0010|0000,00000000       start 172.31.32.0 end 172.31.47.255          ap-southeast-2c
#... extend and map your custom subnet into one AZ

# default us-west-2 subnets    each subnet maps to an AZ
172.31.0.0/20                       us-west-2c  # not sure why 'c' map for lower addreeses
172.31.16.0/20                      us-west-2a
172.31.32.0/20                      us-west-2b
172.31.48.0/20                      us-west-2d
```


## Security Group

* Apply on instance level (technically ENI level) compared to the NACLs which applys on subnet level
* Statefull, so allow request = allow response
* By default deny all, No explicit deny (compared to NACLs which supports explicit deny), only **allow** (which is like **implicit deny**)
* Support IP/CIDR, logical resource, other security group and itself. When sg is used in Source, it means "any instace in the sg"
* Attched to Elastic network interfaces (`ENI`, which is associated with an instance) not instances itself (even if the UI shows it this way). Remember an EC2 instance can have one primary private IP (used in the internet gateway) and multiple ENIs (each ENI has one private IP).

```bash
# Inbound Rules (1)

#   Type           Protocol      Port range     Source       Description-optional
# Custom TCP         TCP            443         sg-xx               xxx

# Outbound Rules (1)

#   Type           Protocol      Port range     Source       Description-optional
#   All traffic      ...
```

## Network Access Control Lists (NACL)

* Stateless and created on **VPC** level and apply on **subnet** level 
* traffic between same subnet won't be impacted by NACLs (compared to SG, traffic between same subnet will be impacted)
* Allow explicit deny (compared to SG which supports explicit deny)

Default NACLs (automatically created when a subnet is created) for inbound and outbound as below shows:

```bash
# Inbound Rules (2)
# Rule Number                Type           Protocol      Port range     Source             Allow/Deny
# 100                      All trafic         All            All        0.0.0.0/0             Allow   <----------default one which means a subnet is implicitly allow any requests
# 99                       All trafic         All            All        91.75.208.102/32      Deny    <----------block a single IP address, note the usage of /32 prefix
# *                        All trafic         All            All        0.0.0.0/0             Deny    <----------always in there and cannot be removed

# Outbound Rules (2)
# Rule Number                Type           Protocol      Port range     Source        Allow/Deny
# 100                      All trafic         All            All        0.0.0.0/0         Allow   <----------default one which means a subnet is implicitly allow any requests
# *                        All trafic         All            All        0.0.0.0/0         Deny    <----------always in there and cannot be removed
```

Rules are processed **in order**, lowest rule number first. Once a match occurs, processing stops. "*" is an implicit deny if nothing else mathes.
Each network ACL also includes **a rule whose rule number is an asterisk (*). This rule ensures that if a packet doesn't match any of the other numbered rules, it's denied. You can't modify or remove this rule**. Note that default NACLs for a VPC has a default "implict allow" rule (which has rule number 100) which reduces admin overhead while custom NACLs is different.

Custom NACLs is created for a specific VPC and are **initially assoicated with no subnets**, and there is no "implicit allow"  any more:
```bash
# Inbound Rules (1)
# Rule Number                Type           Protocol      Port range     Source        Allow/Deny
# *                        All trafic         All            All        0.0.0.0/0         Deny 

# Outbound Rules (1)
# Rule Number                Type           Protocol      Port range     Source        Allow/Deny
# *                        All trafic         All            All        0.0.0.0/0         Deny 
```
so Custom NACLs are **implicit deny** by default

## Internet Gateway and Route Table

An Internet Gateway (`IGW`) is a horizontally scaled, redundant AWS-managed component that acts as the bridge between your VPC and the public internet.

It performs two main roles:

**Outbound traffic**: Let's instances in your VPC reach the internet (e.g., your EC2 calls out to google.com).

**Inbound traffic**: Let's external clients on the internet initiate a connection into your instance (if allowed by security groups, NACLs, and routes).


`IGW` knows how to map a public IP <-> private IP because of the way AWS assigns and manages public IP addresses in a VPC:

1.  Public IPs in AWS are not directly on the EC2 instance
Even though it looks like your EC2 "has" a public IP (e.g. 54.23.x.x), what’\'s actually on the network interface (`ENI`) inside the VPC is only the private IP (e.g. 10.0.1.15).
The public IP is logically attached to that private IP at the VPC level, not directly bound inside the VM.

2. `IGW` maintains a 1:1 mapping table

When you associate a public IP (either auto-assigned public IP or an Elastic IP in EC2 launching) to an ENI's private IP, AWS updates the VPC's IGW mapping table like this:

Public IP   <->   Private IP
54.23.x.x   <->   10.0.1.15

This mapping is stored and maintained by AWS networking infrastructure, not inside your instance. And note that this mapping is not stored in `IGW`, the mapping is stored in VPC data plane (need to learn Advanced AWS Course to understand),  IGW just queries the VPC's internal mapping when performing address translation.

3. Inbound Flow

When a packet comes in from the internet:

Destination: 54.23.x.x (your EC2's public IP).

The IGW checks its mapping table:

“Ah, 54.23.x.x belongs to ENI private IP 10.0.1.15.”

It rewrites the destination address to 10.0.1.15 and sends the packet into the VPC router. The EC2 instance receives it as if it were addressed directly to its private IP.


```bash
# You create this custom VPC

# Name       # VPC ID                 # IPv4 CIDR
demo-vpc     vpc-0bf83577291f3aaab    10.77.0.0/16
```

```bash
# Route Table

# Destination              Target
10.77.0.0/16               local       # <---------------the route table and this entry is automatic created when your custom VPC is created
0.0.0.0/0                  igw-demovpc
```


## Subnets

A subnetwork of a VPC, **1 subnet can only be placed in 1 AZ, a subnet can never span over multiple AZs**, while an AZ can contain multiple subnets

There are 5 reserved IP address in every subnet which cannot be used, for example  `10.16.0.0/20`
* `10.16.0.0`- Network Address that represents the subnet itself
* `10.16.0.1`- VPC Router
* `10.16.0.2`- Reserved DNS
* `10.16.0.3`- Reserved for future use
* `10.16.31.255`- Broadcast Address

**Public Subnet** VS **Private Subnet**

When you create a Subnet under a VPC, the Subne is automatically associated with a newly created "default" Route Table that contains local, and igw-demovpc routes like above. 

So you if you remove `0.0.0.0/0  igw-demovpc` entry, then it becomes a private subnet. So a private subnet is a subnet that doesn't communicate to IGW,

**Public Subnet**:
```bash
# Route Table

# Destination              Target
10.77.0.0/16               local  
0.0.0.0/0                  igw-demovpc
```

**Private Subnet**:
```bash
# Route Table

# Destination              Target
10.77.0.0/16               local  
```



## NAT Gateway

NAT Gateway needs to be in **Public Subnet**:

Private EC2 -> NAT -> Internet Gateway -> Internet

```bash
# Route Table for Private Subnet

# Destination              Target
10.77.0.0/16               local       
0.0.0.0/0                  nat-demovpc
```

note that NAT needs a Elastic IP, so when you delete the NAT gateway, AWS won't automatically release the Elastic IP and keeps on charging you! You have to manually release this Elastic IP after you delete NAT!


## VPC Endpoints

VPC Endpoints allows us to connect VPC to another AWS services OR other supported services over AWS private network rathen than via Public Internet

1. **Gateway Endpoint**: (VPC Level, you cannot choose Subnets in aws console) targets specific IP routes in VPC route table, in the form of a prefix-list, used for traffic destined to DynamoDB or S3 (that's the only 2 services that supported by Gateway Endpoint).

```bash
# Route Table for Private Subnet with NAT:

# Destination                  Target
10.0.0.0/16                    local       
0.0.0.0/0                  nat-0123456789
pl-63a5400a S3           vpce-0abc123456789def0    #<--------------automatically added by AWS in Gateway Endpoint creation
pl-1234abcd DynamoDB     vpce-0aaa987654321aaaa   

# Now all S3 traffic goes via the VPC endpoint, not NAT.
```

```bash
# Route Table for Public Subnet with IGW:

# Destination                  Target
10.0.0.0/16                    local       
0.0.0.0/0                   igw-0abcd1234
pl-63a5400a S3           vpce-0abc123456789def0   #<--------------automatically added by AWS in Gateway Endpoint creation
pl-1234abcd DynamoDB     vpce-0aaa987654321aaaa   

# Now all S3 traffic goes via the VPC endpoint, not IGW.
```

note that when you create VPC endpoints, for the route table you select AWS will automatically add the prefix list route. And for above scenario that want to conmmunicate with S3 and DynamoDB, you need to create two separate Gateway Endpoints, one for S3 and one for DynamoDB, there is no a single Gateway Endpoint that supports both at the same time


2. **Interface Endpoint**: (Subnet Level, you can choose subnets in aws console), rest of things are the same as Gateway Endpoint, except that Interface Endpoint create an ENI in the subnets you choose. If you pick 3 subnets, then this Interface Endpoint creates 3 ENIs. Note that with ENI you are able to create SG against it. You need one interface endpoint per service like Gateway Endpoiont

note that AWS Console UI is weird, you cannot explicitly choose Gateway or Interface in the beginning, you have to choose a service first, if you choose S3 or DynamoDB, then you can pick Gateway endpoint

3. **Endpoint Service**: client side use `Interface Endpoint`, provider side (doesn't need to be the same AWS account as client) creates a Endpoint Service which consists of NLB, targets, client choose the Endpint Service name in its Interface Endpoint


## EC2

No public ipv4  ip address is assigned to e.g EC2 instance, **public IP addresses are associated with an Elastic Network Interface (ENI), not EC2 instance itself** (even though it makes you think ip address is assigned to EC2 instance directly as the choose box says "Auto-assign public IP" in EC2 Launching). An instance can have multiple ENIs. and  `IGW` assoicates the ENI's private address with a public ip address via translation (this process is called **static NAT**), so OS won't be able to see the public IPv4 address. However for IPv6, public ip addresses are directly assigned to instances (underlying OS can see ipv6 public address). 

Note that if you launch EC2 in a **private subnet** with "Auto-assign public IP" in EC2 Launching, even though you get a public ip address of the EC2 instance but you still cannot connect the your EC2 instance because it is in private subnet

# Access EC2 Instance in Private Subet via EC2 Instance in Public Subnet

```bash
# from host to login into EC2 instance in Public Subnet first
 ssh -i ec2-key.pem ec2-user@3.27.124.79  # 3.27.124.79 is the public ip of the EC2 instance in Public Subnet

 # copy host's ec2-key.pem into this EC2 instance in Public Subnet and `chmod 400 ec2-key.pem` in the logged-in EC2 instance

 [ec2-user@ip-3-27-124-79 ~]$ ssh -i ec2-key.pem 10.77.4.54   # 10.77.4.54 is the private ip of the EC2 instance in Private Subnet, 
                                                              # note that ec2-user is not needed since this user from EC2 instance in Public Subnet is already ec2-user
```

**EC2 Purchase Options**

**On-Demand**: (analogy PayAsYouGo sim card), most common way, bill per sec
  You can purchase on-demand capacity reservations (minimum commitment is 14 days) to avoid the situation when AWS doesn't have enough readily available compute power to fulfill your request for the chosen instance type, when you reserve capacity for on demand, you are charged by bill per sec and you are still charge when the instance is stopped

**Reserved Instance**: (analogy "365 long expiry sim card") you know you will ALWAYS be running 20 servers of m4.2xlarge type for 1 year, then buy reserved instances for them; huge discount up to 75% off compared to On-Demand; note that you will be charged even you don't use/start these instances.

**Spot Instance**: Bid like Auction, once the price falls to the minimum you set, it starts to run and it can be stopped by aws when the price goes up; suitable for apps have flexible start and end time; up to 80% off compared to On-Demand

**Saving Plan**: discounts against On-Demand pricing, not separate pricing or instance reservations. It has two major sub options: **Compute Saving Plans** and **EC2 instance Saving Plan** (more discount as you give up flexibility), the former is more flexiable that you can use discount on different EC2 family, region etc while later is binded to a specfic region, EC2 family 

e.g. let's assume we run 20vCPU for one hour:

On-Demand cost: $0.10 per vCPU-hour, $2 for 20 vCPUs

Savings Plan discounted rate: $0.06 per vCPU-hour (40% cheaper), $1.2 for 20 vCPUs

Average usage	    Commitment	    Savings applied	                                                                                   Total Cost
---------------------------------------------------------------------------------------------------------------------------------------------------------
20 vCPUs ($2/hr)	 $2/hr	       No Saving compared to On-Demand, waste unused usage                                                     $2.0
20 vCPUs ($2/hr)	 $1.2/hr	     Full saving                                                                                             $1.20     <--------------------best value
20 vCPUs ($2/hr)	 $1/hr	       $1/hr used up for first 50min ($1/$1.2×60) + 10min ($2/6=$0.33) charged by on-demand rate	             $1.33

so you need to enter you Commitment ($x per hour) neither too high (pay for what you don't use) or two low (not saving enough)

**Dedicated Instance**: no other AWS Account will run an instance on the same Host, but other instances (both dedicated and non-dedicated) from the same AWS Account might run on the same Host. Just like you're hiring a small laptop. if you stop/restart that laptop, next time, you're hiring another laptop and only you can use this laptop.

**Dedicated Host**: No Per Instance Charge, since you already purchase the whole physical server; used for requirements of licensing based on sockets/cores. Similar to dedicated instance, but guess what, that laptop is yours, forever, as long as you pay the bill. You stop/restart that laptop, next time you still boot on the same laptop. But usually this laptop is huge, and the price is a lot more expensive; suitable for software has licensing requirements (per-core, per-socket etc),



## EBS

**Elastic Block Store** is AZ specfic, can only be attached to one EC2 instance (only Provisioned IOPS supports multi-attach) , you can think it as "External HardDrive"

Since `EBS` is AZ specfic, if you want to "copy" volumn from one AZ to another AZ or even another Region (you want to run another EC2 in a different AZ/Region with same data), use "Snapshot" functionaility to create a copy (stored in S3) and then restore a EBS from the snapshoot in other AZ

● **IOPS**: Input / Output operations per second 
  Analogy: How many lanes does a freeway has
● **Throughput**:  MB/s or MiB/s 
  Analogy: How much cago space does a car can carry on the freeway

We know that AWS does not replicate EBS across multiple AZs automatically, but AWS does replicate your EBS on multiple physical servers within that SAME AZ (**AZ-internal replication**).


**General Purpose SSD**
┌────────────┬────────────────────────────────────────────┬───────────────────────────────────────────────┐
│            │             gp3                            │                  gp2                          │
├────────────┼────────────────────────────────────────────┼───────────────────────────────────────────────┼
│            │     start with mininum 3000 IOPS,          │   3 IOPS/GiB (minimum 100 IOPS) to a maximum  │
│   IOPS     │     can purchase extra IOPS up to          │   of 16k IOPS; cannot purchase IOPS since     │
│            │     80k IOPS maxinum                       │   it is pro-rata to size                      │
├────────────┼────────────────────────────────────────────┼───────────────────────────────────────────────┤
│            │                                            │                                               │
│            │   Min Bandwidth:  125 MiB/s                │   Min Bandwidth: 128 MiB/s (1 GiB to 1 TiB)   │
│            │   Max Bandwidth: 2000 MiB/s                │   Max Bandwidth: 250 MiB/s ( > 1 TiB)         │
│ Throughout │                                            │                                               │
│            │   default is 125 MiB/s, can purchase       │                                               │
│            │   a custom bandwidth e.g. 1024 MiB/s as    │   Bandwidth is controlled by aws by size,     │
│            │   long as it is capped at max bandwidth    │   cannot enter a custom bandwidth             │
│            │                                            │                                               │
├────────────┼────────────────────────────────────────────┼───────────────────────────────────────────────┤
│ Generation │         Newer and cheaper                  │               Older                           │
└────────────┼────────────────────────────────────────────┴───────────────────────────────────────────────┘
                                                                                                                            
gp2 provides 3 IOPS/GiB, so only when provisioned at 1TiB does it hit the default performance of gp3: so, for all volumes that are smaller than 1TiB, **it is a no-brainer to use gp3 rather than gp2**. Also note that even though gp3 can allow customer to enter/purchase custom IOPS, it is stil not under category of Provisioned IOPS, because AWS doesn't guarantee IOPS, gp3 needs to share underlying resource with other customers, it is like highway speed limit 100km/h, most of time you can go 100km/h but if there is a congestion, then you can't. However, "Provisioned IOPS" is like AWS reserve a 100km/h lane just for you, you don't compete with other cars


**Provisioned IOPS**: Highest performance SSD volume designed for mission critical application workloads such as databases; support multi-attach to upto 16 EC2 instances in same AZ
┌────────────┬────────────────────────────────────────────┬───────────────────────────────────────────────┬───────────────────────────────────────────────┐                            
│            │           io2 Block Express                │                  io2                          │                  io1                          │                            
├────────────┼────────────────────────────────────────────┼───────────────────────────────────────────────┼───────────────────────────────────────────────┼                            
│  IOPS      │           maxinum 256k IOPS                │            maxinum 64k IOPS                   │            maxinum 64k IOPS                   │       
├────────────┼────────────────────────────────────────────┼───────────────────────────────────────────────┼───────────────────────────────────────────────┤                           
│ Throughout │        Max Bandwidth: 4000 MiB/s           │          Max Bandwidth: 1000 MiB/s            │         Max Bandwidth: 1000 MiB/s             │                            
├────────────┼────────────────────────────────────────────┼───────────────────────────────────────────────┼───────────────────────────────────────────────┤  
│  Failure   │             0.001%                         │                 0.001%                        │                 0.1%                          │                                   
├────────────┼────────────────────────────────────────────┼───────────────────────────────────────────────┼───────────────────────────────────────────────┤                            
│ Generation │         Most expensive tier                │           cheaper than io1                    │               Older                           │                            
└────────────┼────────────────────────────────────────────┴───────────────────────────────────────────────┴───────────────────────────────────────────────┘                                       
For Provisioned IOPS SSD, throughput is directly determined by the IOPS and volume size, not a separately configurable property (unlike gp3 customers can specify a custom throughout), AWS automatically computes the throughput capacity for you based on what you provisioned
Also note that you can't explicitly "choose" io2 Block Express because AWS automatically enables Block Express architecture behind the scenes when certain conditions are met.


**Hard Disk Drives**
┌────────────┬────────────────────────────────────────────┬───────────────────────────────────────────────┐
│            │        Throughput Optimized HDD            │                  Cold HDD                     │
├────────────┼────────────────────────────────────────────┼───────────────────────────────────────────────┼
│   Type     │                  st1                       │                    sc1                        │
├────────────┼────────────────────────────────────────────┼───────────────────────────────────────────────┤
│   IOPS     │           maxinum 500 IOPS                 │              maxinum 250 IOPS                 │
├────────────┼────────────────────────────────────────────┼───────────────────────────────────────────────┤
│ Throughout │             500 MiB/s                      │                  250 MiB/s                    │
├────────────┼────────────────────────────────────────────┼───────────────────────────────────────────────┤
│ User Case  │         big data, data warehouse           │    lowest cost; for infrequently accessed     │
└────────────┼────────────────────────────────────────────┴───────────────────────────────────────────────┘

note that `st` stands for "Streaming Throughput" (that's why it's for sequential, large-volume data).

**Instance Store** volumns are physically attached to EC2 host (offer highest-performance compare to network based gp2 etc), and they are non-persistent so if you stopped, hibernated, or terminated your EC2 instance, the instance store data is gone (reboot is fine). **Instance Store can only be attached at launch time (exam question)** while other EBS can be attached after EC2 instance is created


## EFS (Elastic File System)

Why we need EFS when we already have EBS? 
**EFS automatically scale** in size while **EBS requires users to choose a fixed size**; EBS is raw block storage and when you use it you need to format it, while EFS doesn't need formating

EFS handles concurrency nativelywhile EBS don't. EFS itself implements and enforces file locking, metadata consistency, and concurrent I/O control — it's built into the service.

That's why **EFS can be mounted to multiple EC2 instances** and **EBS normally can only be mounted to a single EC2 instance**

When an EFS is created, a **mount target** is craeted in each AZ for a subnet, a mount target has private ip address such as 10.0.1.32 that enables EC2 instances or other resources to connect to your EFS using the NFS protocol.

┌─────────────────────────────────┬─────────────┐ 
│         Consideration           │    Price    │ 
├─────────────────────────────────┼─────────────┤ 
│           1 TB EFS              │    $307     │ 
├─────────────────────────────────┼─────────────┤ 
│           1 TB EBS              │    $102     │  
├─────────────────────────────────┼─────────────┤ 
│           1 TB S3               │    $24      │ 
┴─────────────────────────────────┴─────────────┘

Note that EBS is faster than EFS; EFS you only pay for what you use.

EFS uses a distributed, POSIX-compliant file system managed by AWS, it does not expose a specific underlying file system type (like ext4 or XFS) to users; instead, it presents a network file system interface via the **NFS** protocol, the name "EFS" is the file system (not open source, proprietary file system provided by AWS) so users only need to mount EFS and use NFS protocol to communicate with it, that's it.

Also note that Windows-based server doesn't use NFS protocol (Windows uses `SMB` protocol for remote file access like NFS in Linux Windows' file system is **NTFS**, Windows's file system is **NTFS**), so you cannot use EFS in Windows Server.
However, AWS offer **FSX** service called FSX for Windows File Server that allows you use FSX for Windows Servers. 

You might ask what's the difference between EFS and FSX, the answer is:
1. You don't set a size for EFS and you do need to set size for FSX
2. You cannot tune IOPS, Throughput on EFS, but you can tune them in FSX, since FSX is a high performancing File Systems. E.g. FSx for **Lustre**, Lustre is a high-performance file system for compute workloads such as machine learning



# EC2 Categories

example instance type: `R5dn.8xlarge`:
`R` is the *Instance Family*, `5` is *Instance Generation*, `8xlarge` is *Instance Size*. `dn` is *Additional Capabilities* (`d` means dense for NVMe, `n` means network optimized )


* **General Purpose**: default, diverse workloads, equal resource ratio:

  `A1`, `M6g`: 'A1' (Graviton), 'M6g' (Graviton 2), Graviton is a family of 64-bit ARM-based CPUs designed by AWS
  `T3`, `T3a`: 'T' means Turbo (Burstable), cheaper assuming low levels of usage, with occasional peaks.
  `M5`, `M5a`, `M5n`: 'M' Stands for "Main"/ "General Purpose", steady state workload alternative to T3

* **Compute Optimized**: Machine Learning, gaming, scientific modelling:

  `C5`, `C5n`: general machine learning, gaming server

* **Memory Optimized**: large in-memory dataset process and database workloads

  `R5`, `R5a`: real time analytics, in-memory caches, certain DB application (in-memory operation)
  `X1`, `X1e`: large scale in-memory applications, lowest $ per GB memory in AWS

* **Storage Optimized**: massive IO operations per second, data warehousing, analytics workloads

   `I3`, `I3en`: high performance NVMe
   `D2`: dense storage (HDD), lowest price disk throughput
   `H1`: high throughout balance CPU/Memory

* **Accelerated Computing**: Harware GPU, field programmable gate arrays (FPGAs)


# EC2 Placement Group

You can specify a Placement Groupt when launching an EC2 instance in the console UI.

Background knowledge: Each rach has its own network and power source

1. `Cluster`: instances are placed into **same rack** (same AZ of course) in datacentre, often same host, so lowest latency and 10Gbps speed communication between instances.

2. `Spread`: maximize resilient by placing each instance into **different rack** across mutiple AZs (each instance runs from a different rack. Inside a same AZ, each difference still run on different rack). E.g. 10 servers runs on 10 differnet racks

3. `Partition`: similar to Spread, but it is **maximum 7 partition group per AZ**, inside a partition group, you can lauch as many as instance you want. Each partition group has its own rack. You can choose the partition to lauch an instance or auto placed; e.g. 12 servers runs on 3 partitions, each partition has 4 servers which are one same rack

===============================================================



## S3 

S3 Storage Classes:

R3AZs : replicated to 3 AZs
DRF   : data retrievals fees per GB
DRT   : data retrievals time
DT    : data transfer fees, Data Transfer In is free, Data Transfer OUT incurs fees, apply on all classes, so it won't be listed in the different classes below
NP    : no public access like S3 static website
storage fees and DT fees applys on all classes

* `S3 Standard`:                     R3AZs, immediate access, no DRF
* `S3 Standard - Infrequent Access`: R3AZs, immediate access, DRF, storage cost is half price of `S3 Standard`, but need to pay DRF
* `S3 One Zone - Infrequent Access`: 1 AZ,  immediate access, DRF
* `S3 Glacier - Instant`:            R3AZs, immediate access, higher DRF, it save more if you access data less frequent than S3 Standard - IA
* `S3 Glacier - Flexible`:           R3AZs, DRT 1min to 12 hours, cheaper DRF, less storage fees, NP
* `S3 Glacier - Deep Archive`:       R3AZs, DRT from 12 to 48 hours, cheapest DRF, cheapest storage fees, NP

* `Intelligent Tiering`: move objects automatically between `Frequent Access Tier` and `Infrequent Access Tier`, note that e.g. Frequent Access Tier is almost the same as S3 standard, but since you avoid burden by letting aws to switch tier for you, you have to pay a little bit more for above 2 tiers. Based on usage, default tier is Frequent Access tier, then objects not accessed for 30 days are moved to Infrequent Access tier.

note that **the storage class is assigned per object, not per bucket**. AWS calculates S3 storage costs on a daily basis. 


In term of versioned files, "L" marker in Console UI is not letter 'L' (if you you think L stand for latest, then questions rise:how come there are multiple versioned file have L? Isn't that only a single file should have L?), actually "L" is box-drawing corner character (U+2514) that represent the hierarchy tree, the one without the 'L' is "latest"

A couple of things to note:
1. Deletion Marker only appear when you delete a object with "Show Version" turn off, if you delete a specfic Version ID (Show Version is on), then it will be permanently delete with any deletion marker
2. The Deletion Marker entry has size 0B (it just a marker, not a object, and sits there like other versioned object), no download option is availble, only Delete option is available. This deletion marker is associated with the right down 'L' version object, once deletion marker itself is deleted, the versioned object with 'L' become the latest ('L' is stripped)

# Object Lock

Object Lock needs to have versioning enabled on bucket. Firstly, you enable Object Lock in bucket level where you can choose **Governance Mode** (Users with specific IAM permissions can overwrite or delete protected object ) or **Compliance Mode** (No users can overwrite or delete protected object versions during the retention period). Secondly you see to enable Object Lock on each file. Note that Object Lock makes you cannot delete "versioned" object (those ones with 'L' and the latest version ), you can still soft delete an object (who will have deletion marker and you can delete the deleltion maker too), which means you can "delete" (soft delete) a object with Show versions turn off.


Common Commands:

```bash
cd "C:\zIT Study\2-Courses\AWS Certified Solutions Associate-Zeal Vora\zFiles"
ssh -i ec2-key.pem ec2-user@3.27.195.218
```


## Elastic Load Balancers

**SLA**: 90%, 99.95% difference

┌───────────┬───────────────────────────────────────────┐ 
│   SLA     │      Downtime per Year and per Day        │ 
├───────────┼───────────────────────────────────────────┤ 
│   90%     │    36 days per year; 2.4 hours per day    │ 
├───────────┼───────────────────────────────────────────┤
│   99%     │    3.6 days per year; 14 mins per day     │  
├───────────┼───────────────────────────────────────────┤
│  99.95%   │    4h22m per year; 43 secs per day        │ 
┴───────────┴───────────────────────────────────────────┘

**RTO**: Recovery Time Objective 
e.g. if RTO is 3 hours, it takes 3 hours for you to recover your infra after disaster occur

**RPO**: Recovery Point Objective
maximum tolerance period to which data can be lost. e.g. if RPO is 5 hours for database, then we should be taking backup of database every five hours. 



* ~~Class Load Balance (`CLB`)~~: 1 SSL per CLB, lacking support in HTTP/2 , path based routing etc; IP addresses as targets, do not use
* Application Load Balancer (`ALB`): HTTP, HTTPS, WebSocket
* Network Load Balacner (`NLB`): TCP, UDP, TLS


When an ELB is deployed into AZs, nodes are created and attached to those AZs, what you see it is an single ELB instance but behind the scene it consists of multiple ELB nodes,  it is configured with an `A` record that resolves to the multiple ELB nodes. So the workflow is like: 


                                                                            ┌─────────────────┐         
                                                                            │                 │         
                                                                            │ ┌───┐           │◄───────┐
                                                                        ┌───│─│   │ ELBNodeA  │  AZA   │
                                                                        │   │ └───┘           │        │
                                                                        │   │                 │        │
   1. Get elb's hostname                                                │   └─────────────────┘        │
user ──────────────────> ELB (created multiple nodes and `A` record)─┬──┤   ┌─────────────────┐        │
 │ ▲                                                                 │  │   │ ┌───┐           │◄───────┤
 │ │     2. ELB evenly return an ELB Node's IP to client             │  └───│─┤   │ ELBNodeB  │        │
 │ └─────────────────────────────────────────────────────────────────┘      │ └───┘           │  AZB   │
 │                                                                          │                 │        │
 │                                                                          └─────────────────┘        │
 │       3.  user sends request to the assigned node's IP address,                                     │
 │       the node further evenly distribute the load to registered group                               │                                          
 └─────────────────────────────────────────────────────────────────────────────────────────────────────┘

note that **`ALB` returns a pool of node ip addresses to client, not the actual backend servers' ip adresses** (not shown in the above diagram), and clients caches the DNS response of an ALB, then starts with the first ip address (the client's OS uses a pointer points to the first address and move to next etc) like round-robin load balancing.


# Application Load Balancer

* Layer 7 (Application), that's why it supports path based routing because it can access HTTP request to make routing decisions

* application server don't see the IP of the client directly, the IP of client is inserted in the header `X-Forwarded-For`, and server also get `X-Forwarded-Port` and `X-Forwarded-Proto` (e.g. https)

* **ALB cannot have one static IP per AZ**, unlike NLB, each ALB node's IP address are dynamic and can change depending on scaling

* Cross-Zone LB enabled by default, no extra charge

* registered target always see ALB's IP address as source address, not client's IP address. If the target e.g. EC2 instance want to know the original source IP address, then it need to check the HTTP headers  `X-Forwarded-For`

the most important thing to note is, there are two TCP connections involved, first TCP connection initialized  by client, ALB keeps this connection open while initializing a second TCP connection between ALB and its target  

Client ---> TCP connection 1 ---> ALB ---> New TCP connection 2 ---> Backend
    <────────────────────────────┘
    ALB return response to client
   via TCP connection 1 (keep open)


This is different to Network Load Balancers (`NLB`), which only one TCP connection is involved, NLB is just like a router that forwards the request to backend, that is why NLB (can handle millions requests per sec)is much faster than ALB

A common case of using ALB:

- you have one ALB with 3 EC2 instance as target group, ALB has its own security group
- you remove public access from all EC2's security group and only allow Inbound from ALB (using ALB's security group)
- you add multiple listener rule based on HTTP request header, query string etc, e.g return a fixed 404 response for /error

# Network Load Balancer

* Layer 4 TCP/UDP, so it is faster than ALB and it can handle millions of request per second with ultra-low latency
* NLB has one **static IP** per AZ (helpful for whiltelisting specific IP), compared to that ALB's non static IP per AZ




## Auto-Scaling Group

**Health Check Grace Period** and **Default Cooldown** are different concepts. Health Check Grace Period applies to a single instance while Default Cooldown applies to the auto scaling group.

If an instance is launched at second 0 because the CPU is above 50%, this new instance will get a grace period of 300 seconds to become healthy. If healthy after 300 seconds it will continue to run, otherwise it will be terminated.

If the CPU is still above 50% at second 100, the auto scaling group will start a new instance (the last scaling action took place 100 seconds ago = cooldown period). Hence, the answer to your question is 100 seconds until a new instance is launched.

Please also note that by default, Amazon EC2 Auto Scaling does not honor the cooldown period during manual scaling activities (= setting desired capacity) and that if an instance becomes unhealthy, the Auto Scaling group does not wait for the cooldown period to complete before replacing the unhealthy instance.


