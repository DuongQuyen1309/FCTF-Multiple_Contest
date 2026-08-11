# Guide: Deploying the FCTF Platform on Google Cloud Platform (GCP)

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Prerequisites](#2-prerequisites)
3. [Phase 1: Create a GCP Project & Basic Configuration](#3-phase-1)
4. [Phase 2: Create VPC Network & Firewall Rules](#4-phase-2)
5. [Phase 3: Create VM Instances](#5-phase-3)
6. [Phase 4: Install the K3s Cluster](#6-phase-4)
7. [Phase 5: Deploy the FCTF Platform](#7-phase-5)
8. [Phase 6: Configure Domain & SSL](#8-phase-6)
9. [Phase 7: Verify & Troubleshoot](#9-phase-7)
10. [Estimated Cost & Optimization](#10-cost)
11. [Cleanup / Deleting Resources](#11-cleanup)
12. [Appendix](#appendix)

---

## 1. Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    Google Cloud Platform                         │
│                                                                 │
│  ┌──────────────────────────────────────────────────────┐       │
│  │              VPC Network: fctf-vpc                    │       │
│  │              Subnet: 10.148.0.0/24                    │       │
│  │                                                       │       │
│  │  ┌─────────────────────┐  ┌─────────────────────┐    │       │
│  │  │   VM1: fctf-master  │  │   VM2: fctf-worker  │    │       │
│  │  │   8 vCPU, 16GB RAM  │  │   8 vCPU, 32GB RAM  │    │       │
│  │  │   200GB SSD         │  │   200GB SSD         │    │       │
│  │  │                     │  │                     │    │       │
│  │  │  • K3s Server       │  │  • K3s Agent        │    │       │
│  │  │  • NFS Server       │  │  • App Workloads    │    │       │
│  │  │  • Calico CNI       │  │  • Challenge Pods   │    │       │
│  │  │  • Ingress Nginx    │  │  • gVisor Sandbox   │    │       │
│  │  │  • Harbor Registry  │  │                     │    │       │
│  │  └─────────────────────┘  └─────────────────────┘    │       │
│  │           │                        │                  │       │
│  │           └────── Private Link ────┘                 │       │
│  │                  10.148.0.x                           │       │
│  └──────────────────────────────────────────────────────┘       │
│                         │                                       │
│              ┌──────────┴──────────┐                            │
│              │   External IP       │                            │
│              │   (Static)          │                            │
│              └──────────┬──────────┘                            │
│                         │                                       │
└─────────────────────────┼───────────────────────────────────────┘
                          │
                    ┌─────┴─────┐
                    │  Internet  │
                    │  Users     │
                    └───────────┘
```

### Resource Requirements

| VM | vCPU | RAM | Disk | Role |
|---|---|---|---|---|
| **fctf-master** | 8 | 16 GB | 200 GB SSD | K3s Server, NFS Server, Harbor |
| **fctf-worker** | 8 | 32 GB | 200 GB SSD | K3s Agent, runs workloads |

> [!NOTE]
> If you want to save cost for test/demo purposes, you can scale down to:
> - Master: 4 vCPU / 8 GB RAM / 100 GB SSD
> - Worker: 4 vCPU / 16 GB RAM / 100 GB SSD
>
> However, this limits the number of challenges you can deploy concurrently.

---

## 2. Prerequisites

### On your Windows machine

- [ ] **Google Cloud Account** — Sign up at [console.cloud.google.com](https://console.cloud.google.com)
- [ ] **Billing Account** — Requires a credit card (the GCP free trial gives $300 credit / 90 days)
- [ ] **Google Cloud CLI (`gcloud`)** — Install from [cloud.google.com/sdk](https://cloud.google.com/sdk/docs/install)

### Installing the gcloud CLI on Windows

```powershell
# Download and run the Google Cloud SDK installer from:
# https://cloud.google.com/sdk/docs/install#windows

# After installation, open a new PowerShell window and log in:
gcloud init

# Then:
# 1. Log in (opens a browser to sign in with Google)
# 2. Select or create a new project
# 3. Choose a default region (asia-southeast1 is closest to Vietnam)
```

---

## 3. Phase 1: Create a GCP Project & Basic Configuration

### 3.1 Create the Project

```powershell
# Create a new project (the project name must be unique across all of GCP)
gcloud projects create fctf-platform --name="FCTF Platform"

# Set the project as the default
gcloud config set project fctf-platform

# Link a billing account
# (Or go to Console > Billing > Link project)
gcloud billing accounts list
gcloud billing projects link fctf-platform --billing-account=YOUR_BILLING_ACCOUNT_ID
```

### 3.2 Enable the required APIs

```powershell
gcloud services enable compute.googleapis.com
gcloud services enable dns.googleapis.com
```

### 3.3 Set the default region/zone

```powershell
# Pick the region closest to Vietnam (Singapore)
gcloud config set compute/region asia-southeast1
gcloud config set compute/zone asia-southeast1-b
```

> [!TIP]
> **Which region should you choose?**
> | Region | Location | Latency from VN |
> |---|---|---|
> | `asia-southeast1` | Singapore | ~30ms ⭐ |
> | `asia-east1` | Taiwan | ~50ms |
> | `asia-northeast1` | Tokyo | ~80ms |
> | `us-central1` | Iowa, US | ~200ms |

---

## 4. Phase 2: Create VPC Network & Firewall Rules

### 4.1 Create the VPC Network

```powershell
# Create a custom VPC
gcloud compute networks create fctf-vpc `
  --subnet-mode=custom `
  --bgp-routing-mode=regional

# Create the subnet
gcloud compute networks subnets create fctf-subnet `
  --network=fctf-vpc `
  --region=asia-southeast1 `
  --range=10.148.0.0/24
```

### 4.2 Create Firewall Rules

```powershell
# 1) SSH access (from your own IP, or 0.0.0.0/0 for testing)
gcloud compute firewall-rules create fctf-allow-ssh `
  --network=fctf-vpc `
  --allow=tcp:22 `
  --source-ranges=0.0.0.0/0 `
  --target-tags=fctf-node `
  --description="Allow SSH"

# 2) HTTP/HTTPS (for web access from the internet)
gcloud compute firewall-rules create fctf-allow-http-https `
  --network=fctf-vpc `
  --allow=tcp:80,tcp:443 `
  --source-ranges=0.0.0.0/0 `
  --target-tags=fctf-master `
  --description="Allow HTTP and HTTPS"

# 3) K3s API server (so the worker can join the cluster)
gcloud compute firewall-rules create fctf-allow-k3s-api `
  --network=fctf-vpc `
  --allow=tcp:6443 `
  --source-ranges=10.148.0.0/24 `
  --target-tags=fctf-master `
  --description="Allow K3s API from internal subnet"

# 4) Internal traffic between nodes (Calico VXLAN, NFS, etc.)
gcloud compute firewall-rules create fctf-allow-internal `
  --network=fctf-vpc `
  --allow=tcp:0-65535,udp:0-65535,icmp `
  --source-ranges=10.148.0.0/24 `
  --target-tags=fctf-node `
  --description="Allow all internal traffic between nodes"

# 5) NodePort services (if you need direct access via NodePort)
gcloud compute firewall-rules create fctf-allow-nodeports `
  --network=fctf-vpc `
  --allow=tcp:30000-32767 `
  --source-ranges=0.0.0.0/0 `
  --target-tags=fctf-master `
  --description="Allow NodePort range"
```

> [!WARNING]
> **On Security:**
> - The SSH rule with `0.0.0.0/0` should only be used for testing. In production, restrict it to your specific IP.
> - To find your current IP: visit [whatismyip.com](https://whatismyip.com), then replace `0.0.0.0/0` with `YOUR_IP/32`.

### 4.3 Reserve a Static IP (for the Master)

```powershell
# Create a Static External IP for the master node
gcloud compute addresses create fctf-master-ip `
  --region=asia-southeast1

# View the created IP
gcloud compute addresses describe fctf-master-ip --region=asia-southeast1 --format="get(address)"
```

> Write this IP down — you will use it for the TLS SAN and DNS records.

---

## 5. Phase 3: Create VM Instances

### 5.1 Create the Master Node

```powershell
gcloud compute instances create fctf-master `
  --zone=asia-southeast1-b `
  --machine-type=e2-standard-8 `
  --boot-disk-size=200GB `
  --boot-disk-type=pd-ssd `
  --image-family=ubuntu-2204-lts `
  --image-project=ubuntu-os-cloud `
  --network=fctf-vpc `
  --subnet=fctf-subnet `
  --address=fctf-master-ip `
  --tags=fctf-node,fctf-master `
  --metadata=startup-script='#!/bin/bash
    apt update && apt install -y git'
```

### 5.2 Create the Worker Node

```powershell
gcloud compute instances create fctf-worker `
  --zone=asia-southeast1-b `
  --machine-type=e2-highmem-8 `
  --boot-disk-size=200GB `
  --boot-disk-type=pd-ssd `
  --image-family=ubuntu-2204-lts `
  --image-project=ubuntu-os-cloud `
  --network=fctf-vpc `
  --subnet=fctf-subnet `
  --tags=fctf-node,fctf-worker `
  --metadata=startup-script='#!/bin/bash
    apt update && apt install -y git nfs-common'
```

### 5.3 Verify the VMs

```powershell
# List the VMs
gcloud compute instances list

# The output will look like:
# NAME          ZONE                  MACHINE_TYPE   INTERNAL_IP   EXTERNAL_IP     STATUS
# fctf-master   asia-southeast1-b     e2-standard-8  10.148.0.2    34.xxx.xxx.xxx  RUNNING
# fctf-worker   asia-southeast1-b     e2-highmem-8   10.148.0.3    35.xxx.xxx.xxx  RUNNING
```

> [!IMPORTANT]
> **Write these IPs down:**
> | VM | Internal IP | External IP |
> |---|---|---|
> | fctf-master | `10.148.0.x` | `34.xxx.xxx.xxx` (static) |
> | fctf-worker | `10.148.0.x` | `35.xxx.xxx.xxx` |
>
> You will need them in the following steps.

### 5.4 Choosing the Right Machine Type

| Machine Type | vCPU | RAM | Price ~USD/month | Notes |
|---|---|---|---|---|
| `e2-standard-8` | 8 | 32 GB | ~$195 | Recommended for the master |
| `e2-highmem-8` | 8 | 64 GB | ~$260 | If the worker needs lots of RAM |
| `e2-standard-4` | 4 | 16 GB | ~$97 | Budget option for the master |
| `e2-highmem-4` | 4 | 32 GB | ~$130 | Budget option for the worker |
| `n2d-standard-8` | 8 | 32 GB | ~$205 | AMD, better performance |

> [!TIP]
> **Saving cost:**
> - Use **Spot VMs** (add `--provisioning-model=SPOT`) → ~60-90% cheaper, but the VM can be reclaimed at any time.
> - Use **Committed Use Discounts** for long-running deployments (1 year → ~37% off, 3 years → ~55% off).
> - Use `e2-medium` (2 vCPU, 4 GB) for quick testing, then scale up later.

---

## 6. Phase 4: Install the K3s Cluster

### 6.1 SSH into the Master Node

```powershell
# From your Windows machine, SSH into the master
gcloud compute ssh fctf-master --zone=asia-southeast1-b
```

> On the first SSH, gcloud automatically generates an SSH key pair. Press Enter when prompted for a passphrase.

### 6.2 On the Master: Clone the repo & Configure

```bash
# Clone the FCTF repository
git clone <repo-url> FCTF
cd FCTF
chmod +x manage.sh

# ===== Step 1: Configure domains/IP =====
./manage.sh
# Choose: 9) Configure service domains/IP
```

When prompted, enter the following values:

| Token | Value | Explanation |
|---|---|---|
| `MASTER_NODE_PRIVATE_IP` | `10.148.0.x` | Master's internal IP (get it from `hostname -I`) |
| `RABBITMQ_DOMAIN` | `rabbitmq.fctf.yourdomain.com` | Or use `rabbitmq.local` if you don't have a domain yet |
| `GRAFANA_DOMAIN` | `grafana.fctf.yourdomain.com` | |
| `CONTESTANT_DOMAIN` | `contest.fctf.yourdomain.com` | Main domain for contestants |
| `ADMIN_DOMAIN` | `admin.fctf.yourdomain.com` | Admin panel domain |
| `ARGO_DOMAIN` | `argo.fctf.yourdomain.com` | |
| `CONTESTANT_API_DOMAIN` | `api.fctf.yourdomain.com` | API endpoint |
| `REGISTRY_DOMAIN` | `registry.fctf.yourdomain.com` | Harbor registry |
| `RANCHER_DOMAIN` | `rancher.fctf.yourdomain.com` | |
| `GATEWAY_DOMAIN` | `gateway.fctf.yourdomain.com` | Challenge gateway |

> [!TIP]
> **If you don't have your own domain yet**, you can use [nip.io](https://nip.io) (free wildcard DNS):
> - `CONTESTANT_DOMAIN` → `contest.34.xxx.xxx.xxx.nip.io`
> - `ADMIN_DOMAIN` → `admin.34.xxx.xxx.xxx.nip.io`
> - ...replace `34.xxx.xxx.xxx` with the master's actual External IP.
>
> Alternatively, use `.local` names and add them to your local machine's hosts file.

### 6.3 On the Master: Update the configuration before installing

> [!CAUTION]
> **REQUIRED before running the master setup.** If you skip this, services will not be able to reach each other.

```bash
# --- 1. Update the NFS server IP in the PV files ---
MASTER_INTERNAL_IP=$(hostname -I | awk '{print $1}')

# Update all PV files
sed -i "s|server:.*|server: ${MASTER_INTERNAL_IP}|g" \
  FCTF-k3s-manifest/prod/storage/pv/admin-mvc-pv.yaml \
  FCTF-k3s-manifest/prod/storage/pv/contestant-be-pv.yaml \
  FCTF-k3s-manifest/prod/storage/pv/up-challenge-workflow-pv.yaml \
  FCTF-k3s-manifest/prod/storage/pv/start-challenge-workflow-pv.yaml

# --- 2. Update the Secrets (REPLACE ALL DEFAULT PASSWORDS) ---
# Open and edit each secret file
nano FCTF-k3s-manifest/prod/env/secret/mariadb-auth-secret.yaml
nano FCTF-k3s-manifest/prod/env/secret/redis-auth-secret.yaml
# ... (edit every file in the secret/ directory)

# --- 3. Review the ConfigMaps (URLs, API endpoints) ---
nano FCTF-k3s-manifest/prod/env/configmap/contestant-be-cm.yaml
nano FCTF-k3s-manifest/prod/env/configmap/challenge-gateway-cm.yaml
# ... (review every file in the configmap/ directory)
```

### 6.4 On the Master: Set up the K3s Server

```bash
# ===== Step 2: Set up the K3s Master =====
./manage.sh
# Choose: 1) Setup master

# When prompted:
#   Master TLS SAN: enter the master's EXTERNAL IP (e.g. 34.124.131.240)
#   NFS allowed subnet: enter 10.148.0.0/24 (the private subnet range)
```

The script automatically:
1. ✅ Updates system packages
2. ✅ Configures kernel modules (br_netfilter, overlay)
3. ✅ Disables swap
4. ✅ Sets up the NFS server at `/srv/nfs/share`
5. ✅ Installs the K3s server
6. ✅ Installs gVisor (runsc) for sandboxing
7. ✅ Installs Calico CNI (VXLAN mode)
8. ✅ Applies the RuntimeClass for gVisor
9. ✅ Taints the master node

```bash
# Verify the K3s server is running
kubectl get nodes
# NAME               STATUS   ROLES                  AGE   VERSION
# server-1-master    Ready    control-plane,master   1m    v1.xx.x+k3s1

# Get the master token (needed for the worker to join)
./manage.sh
# Choose: 7) Get master token
# Copy the token from the output
```

### 6.5 SSH into the Worker Node & Join the Cluster

```powershell
# Open a NEW terminal on Windows and SSH into the worker
gcloud compute ssh fctf-worker --zone=asia-southeast1-b
```

```bash
# On the worker node
git clone <repo-url> FCTF
cd FCTF
chmod +x manage.sh

# ===== Set up the Worker =====
./manage.sh
# Choose: 2) Setup worker

# When prompted:
#   Master URL: https://10.148.0.x:6443   (the master's INTERNAL IP)
#   Master token: <paste the token from the previous step>
```

### 6.6 Verify the Cluster (back on the Master)

```bash
# SSH back into the master
kubectl get nodes -o wide
# NAME               STATUS   ROLES                  AGE   VERSION   INTERNAL-IP
# server-1-master    Ready    control-plane,master   5m    v1.xx     10.148.0.2
# worker-1           Ready    <none>                 1m    v1.xx     10.148.0.3
```

---

## 7. Phase 5: Deploy the FCTF Platform

### 7.1 Install FCTF (on the Master)

```bash
# ===== Step 3: Install FCTF =====
./manage.sh
# Choose: 3) Install FCTF
```

The `apply-fctf.sh` script automatically performs:

| Step | Description | Estimated time |
|---|---|---|
| 1 | Create namespaces (app, db, argo, storage) | ~5s |
| 2 | Apply Secrets & ConfigMaps | ~5s |
| 3 | Apply PVs & PVCs | ~5s |
| 4 | Install Helm (if not already present) | ~30s |
| 5 | Deploy Helm charts via `prod/helm.sh` (ingress-nginx, cert-manager, MariaDB, Redis, RabbitMQ, Argo Workflows, Loki, Prometheus, **Harbor**, Rancher) | ~5-10 min |
| 6 | Deploy the 7 app services | ~2 min |
| 7 | Apply NetworkPolicies | ~5s |
| 8 | Apply Ingress rules | ~5s |
| 9 | Apply CronJobs & Argo templates | ~5s |
| 10 | Bootstrap RabbitMQ users | ~1-2 min |
| 11 | Initialize the MariaDB schema & grants | ~1 min |
| 12 | Rotate service passwords | ~2 min |

### 7.2 Monitor Progress

```bash
# Watch pods being created
watch kubectl get pods -A

# Wait until ALL pods are in Running/Completed state
# The first run can take 10-20 minutes (pulling images)
```

Expected state:

```
NAMESPACE   NAME                                    READY   STATUS
app         admin-mvc-xxx                           1/1     Running
app         contestant-be-xxx                       1/1     Running
app         contestant-portal-xxx                   1/1     Running
app         deployment-center-xxx                   1/1     Running
app         deployment-consumer-xxx                 1/1     Running
app         deployment-listener-xxx                 1/1     Running
app         challenge-gateway-xxx                   1/1     Running
db          mariadb-0                               1/1     Running
db          redis-master-0                          1/1     Running
db          rabbitmq-0                              1/1     Running
argo        argo-workflows-server-xxx               1/1     Running
...
```

### 7.3 Set up Harbor (Optional but Recommended)

Harbor itself was already installed in 7.1 (by `prod/helm.sh`, into namespace `registry`). This step creates the project and robot account, wires the pull secrets, and pushes the first images.

```bash
# ===== Step 4: Set up Harbor =====
./manage.sh
# Choose: 4) Setup harbor
```

The `setup-harbor.sh` script performs:

| Step | Description | Estimated time |
|---|---|---|
| 1 | Install dependencies + Docker Engine (skip with `INSTALL_DOCKER=false`) | ~1-2 min |
| 2 | Wait for `https://<REGISTRY_DOMAIN>/v2/` to respond (max 180s) | ~10s |
| 3 | **Pause** — you create the project and robot account in the Harbor UI | manual |
| 4 | Prompt for the robot username/secret (or pass `CI_USER` / `CI_PASS`) | — |
| 5 | Apply pull secrets: `regcred` (app), `global-regcred` + `docker-registry-creds` (argo) | ~5s |
| 6 | `docker login`, then build & push 8 images to `<REGISTRY_DOMAIN>/fctf` | ~15-30 min |

> [!IMPORTANT]
> DNS for `REGISTRY_DOMAIN` must already point at the master (Phase 6) before you run this — the script exits if Harbor does not answer within 180s. The UI step is mandatory: create a **private** project named exactly `fctf`, plus a robot account with **pull + push** permission.

```bash
# Afterwards, restart the app so it pulls the freshly pushed images
kubectl -n app rollout restart deployment --all
```

> [!NOTE]
> Harbor stores the images for both the platform services and the challenges. The default credentials in `harbor-values.yaml` (`harborAdminPassword: "FCTF@2025"`, DB and registry passwords) should be changed back in section 6.3. If you skip Harbor, you can use DockerHub or Artifact Registry instead — but you then have to create those three pull secrets yourself.

### 7.4 Set up CI/CD (Optional)

Creates a least-privilege ServiceAccount for GitHub Actions and prints a base64 kubeconfig for it.

```bash
# ===== Step 5: Set up CI/CD =====
./manage.sh
# Choose: 5) Setup CI/CD
```

The `cicd-setup.sh` script performs:

| Step | Description |
|---|---|
| 1-3 | ServiceAccount `cicd-deployer` in namespace `app`, plus Role/RoleBinding (patch deployments only) |
| 4 | Create a long-lived token Secret |
| 5 | Build a standalone kubeconfig — **prompts for a public IP**, since K3s' local one is `127.0.0.1` |
| 6 | Print the base64 kubeconfig for the GitHub secret `KUBE_CONFIG` |

Then add these GitHub secrets (`Settings → Secrets and variables → Actions`):

| Secret | Value |
|---|---|
| `HARBOR_USERNAME` | Harbor username or robot name |
| `HARBOR_TOKEN` | Harbor access token / robot secret (not the account password) |
| `KUBE_CONFIG` | The base64 output from the script |

The pipeline ([.github/workflows/ci-cd.yml](.github/workflows/ci-cd.yml)) triggers on push to `main`, rebuilds only the services whose files changed, pushes `:<sha>` and `:latest` to Harbor, then runs `kubectl set image` + `rollout status` in namespace `app`.

> [!IMPORTANT]
> Three things are required for the pipeline to actually reach the cluster:
> - Enter the master's **External IP** at the script's prompt — pressing Enter leaves `127.0.0.1` and every deploy times out. That IP must also be in the master's TLS SAN (section 6.4).
> - Open port `6443` to the runners; the Phase 2 firewall rule only allows the internal subnet.
> - Edit the registry domain hardcoded in `ci-cd.yml` (`env.REGISTRY`, `env.IMAGE_BASE`, and the `Login to Harbor` step) — `./manage.sh → 9` does not rewrite that file.

---

## 8. Phase 6: Configure Domain & SSL

### Option A: Use Your Own Domain (Recommended for Production)

#### 8.1 Buy or use an existing domain

You need to point DNS records at the master node's External IP.

#### 8.2 Configure DNS Records

Go to your DNS provider (Cloudflare, Google Domains, Namecheap, etc.) and create these **A Records**:

| Type | Name | Value | TTL |
|---|---|---|---|
| A | `contest.fctf` | `34.xxx.xxx.xxx` (Master External IP) | 300 |
| A | `admin.fctf` | `34.xxx.xxx.xxx` | 300 |
| A | `api.fctf` | `34.xxx.xxx.xxx` | 300 |
| A | `gateway.fctf` | `34.xxx.xxx.xxx` | 300 |
| A | `argo.fctf` | `34.xxx.xxx.xxx` | 300 |
| A | `grafana.fctf` | `34.xxx.xxx.xxx` | 300 |
| A | `rabbitmq.fctf` | `34.xxx.xxx.xxx` | 300 |
| A | `registry.fctf` | `34.xxx.xxx.xxx` | 300 |
| A | `rancher.fctf` | `34.xxx.xxx.xxx` | 300 |

> Replace `fctf` with your actual domain, e.g. `contest.fctf.example.com`

#### 8.3 SSL Certificates (automatic via cert-manager)

The project is already configured with **cert-manager** + **Let's Encrypt**. Once DNS points correctly, certificates are issued automatically.

```bash
# Verify certificates
kubectl get certificates -A
kubectl get certificaterequests -A

# Check the status
kubectl describe certificate -n app <certificate-name>
```

### Option B: Use nip.io (Fast, for Test/Demo)

No domain purchase required. Uses a free wildcard DNS service.

```bash
# Example: Master External IP = 34.124.131.240
# These domains resolve automatically:
# contest.34.124.131.240.nip.io  → 34.124.131.240
# admin.34.124.131.240.nip.io    → 34.124.131.240
```

> [!WARNING]
> nip.io **does NOT support SSL/HTTPS** via Let's Encrypt (domain ownership cannot be verified).
> Use HTTP only, for testing.

### Option C: Use Google Cloud DNS (Managed DNS)

```powershell
# Create a DNS zone on GCP
gcloud dns managed-zones create fctf-zone `
  --dns-name="fctf.yourdomain.com." `
  --description="FCTF Platform DNS"

# Add A records
gcloud dns record-sets create contest.fctf.yourdomain.com. `
  --zone=fctf-zone `
  --type=A `
  --ttl=300 `
  --rrdatas=34.xxx.xxx.xxx

# Repeat for the other subdomains...

# Get the nameservers → update them at your domain registrar
gcloud dns managed-zones describe fctf-zone --format="get(nameServers)"
```

---

## 9. Phase 7: Verify & Troubleshoot

### 9.1 Check the entire system

```bash
# 1. Nodes
kubectl get nodes -o wide

# 2. All Pods
kubectl get pods -A

# 3. Services
kubectl get svc -A

# 4. Ingress
kubectl get ingress -A

# 5. PV/PVC
kubectl get pv,pvc -A

# 6. Events (view recent errors)
kubectl get events -A --sort-by='.lastTimestamp' | tail -20

# 7. TLS certificates (all should be READY=True)
kubectl get certificates -A

# 8. gVisor RuntimeClass (required by challenge pods)
kubectl get runtimeclass

# 9. Argo workflows
kubectl get wf -n argo
```

### 9.2 Access a Service (if you don't have a domain yet)

```bash
# Port-forward from the master
kubectl port-forward -n app svc/contestant-portal 8080:80 --address=0.0.0.0 &
kubectl port-forward -n app svc/contestant-be 5000:80 --address=0.0.0.0 &
kubectl port-forward -n app svc/admin-mvc 4000:8000 --address=0.0.0.0 &

# Access from a browser:
# http://MASTER_EXTERNAL_IP:8080  → Contestant Portal
# http://MASTER_EXTERNAL_IP:5000  → API
# http://MASTER_EXTERNAL_IP:4000  → Admin Panel
```

> Remember to add a firewall rule for ports 8080, 5000, and 4000 when port-forwarding:
> ```powershell
> gcloud compute firewall-rules create fctf-allow-portforward `
>   --network=fctf-vpc `
>   --allow=tcp:4000,tcp:5000,tcp:8080 `
>   --source-ranges=0.0.0.0/0 `
>   --target-tags=fctf-master
> ```

### 9.3 Verify the management consoles

| Console | URL | Namespace / Service | Default login |
|---|---|---|---|
| Harbor (registry) | `https://<REGISTRY_DOMAIN>` | `registry` / `harbor:80` | `admin` / `FCTF@2025` |
| Argo Workflows | `https://<ARGO_DOMAIN>` | `argo` / `argo-workflows-server:2746` | nginx basic-auth, then a bearer token |
| RabbitMQ Management | `https://<RABBITMQ_DOMAIN>` | `db` / `rabbitmq:15672` | `rabbit-admin` / `Fctf2025@admin` |
| Grafana | `https://<GRAFANA_DOMAIN>` | `monitoring` / `prometheus-grafana:80` | `admin` / `Fctf2025@` |
| Rancher | `https://<RANCHER_DOMAIN>` | `cattle-system` / `rancher:443` | bootstrap password `Ab@123456789` |

```bash
# Are they running?
kubectl get pods -n registry
kubectl get pods -n argo
kubectl get pods -n db
kubectl get pods -n monitoring
kubectl get pods -n cattle-system

# All five ingresses + their TLS secrets
kubectl get ingress -A
kubectl get certificates -A

# Reach them without DNS (port-forward from the master)
kubectl port-forward -n registry svc/harbor 8081:80 --address=0.0.0.0 &
kubectl port-forward -n argo svc/argo-workflows-server 2746:2746 --address=0.0.0.0 &
kubectl port-forward -n db svc/rabbitmq 15672:15672 --address=0.0.0.0 &
kubectl port-forward -n monitoring svc/prometheus-grafana 3000:80 --address=0.0.0.0 &
kubectl port-forward -n cattle-system svc/rancher 8443:443 --address=0.0.0.0 &
```

> [!NOTE]
> Harbor via port-forward will load the UI but **not** accept `docker push`, because `externalURL` is set to `https://<REGISTRY_DOMAIN>`. Pushing requires the real domain.

> [!CAUTION]
> Every password in the table above is committed in the repo — `harbor-values.yaml`, `rabbitmq-values.yaml`, `prometheus-stack-values.yaml`, `rancher-values.yaml` — and `rotate-service-passwords.sh` deliberately keeps the Harbor admin, `rabbit-admin`, Rancher and Grafana admin accounts unchanged. Change all four by hand after the first login.

### 9.4 Common Troubleshooting

#### Pod stuck in `Pending`

```bash
# See the reason
kubectl describe pod <pod-name> -n <namespace>

# Common causes:
# 1. Insufficient resources → scale up the machine type
# 2. No nodes available → check taints/tolerations
# 3. PVC pending → check the NFS server
```

#### Pod in `CrashLoopBackOff`

```bash
# View the logs
kubectl logs <pod-name> -n <namespace> --previous

# Common causes:
# 1. Wrong connection string → check ConfigMaps/Secrets
# 2. DB not ready yet → wait for the MariaDB pod to start first
# 3. Image pull error → check the image name and registry access
```

#### Services cannot reach each other

```bash
# Test DNS from inside the cluster
kubectl run -it --rm debug --image=busybox -- nslookup mariadb.db.svc.cluster.local

# Check NetworkPolicy
kubectl get networkpolicy -A

# Check Calico
kubectl get pods -n kube-system | grep calico
```

#### NFS mount fails

```bash
# On the master, check the NFS server
sudo exportfs -v
sudo systemctl status nfs-kernel-server

# On the worker, test the mount
sudo mount -t nfs MASTER_INTERNAL_IP:/srv/nfs/share /mnt
ls /mnt
sudo umount /mnt
```

#### Certificate stuck at `READY=False`

```bash
kubectl describe certificate -n <namespace> <name>
kubectl get order,challenge -A          # ACME progress
kubectl logs -n cert-manager deploy/cert-manager

# Common causes:
# 1. DNS not pointing at the master yet → the HTTP-01 challenge cannot be reached
# 2. Port 80 blocked → HTTP-01 needs it open, not just 443
# 3. Rate limited by Let's Encrypt (see the warning below)
```

> [!WARNING]
> The ClusterIssuer is named `letsencrypt-dev` but points at the **production** ACME endpoint (`acme-v02.api.letsencrypt.org`), so real rate limits apply — 50 certificates per registered domain per week, and 5 duplicates of the same set. Repeatedly reinstalling while DNS is still wrong can lock you out for a week. While testing, switch `spec.acme.server` in [cluster-issuer.yaml](FCTF-k3s-manifest/prod/cert-manager/cluster-issuer.yaml) to `https://acme-staging-v02.api.letsencrypt.org/directory` (the certificate will not be browser-trusted, but issuance is unlimited). Also change `spec.acme.email` — it is committed as a developer's address, so expiry notices go to them.

#### Challenge pods fail to start (gVisor)

Challenge pods run under the `runsc` RuntimeClass, which only exists if gVisor was installed on the node running them (`setup-worker.sh` does this by default).

```bash
# From the master
kubectl get runtimeclass
kubectl describe pod -n <namespace> <challenge-pod>

# On the worker
runsc --version
sudo grep -A3 runsc /var/lib/rancher/k3s/agent/etc/containerd/config.toml

# "runsc: executable file not found" → gVisor is missing on that node.
# Re-run setup-worker.sh with --install-gvisor true
```

#### Argo workflow fails (challenge deploy/start)

```bash
kubectl get wf -n argo
kubectl describe wf -n argo <workflow-name>
kubectl logs -n argo <workflow-pod> --all-containers

# Common causes:
# 1. Push denied → docker-registry-creds is missing or the robot lacks Push (section 7.3)
# 2. NFS PVC not bound → check the workflow PVs
# 3. RabbitMQ message never consumed → check deployment-consumer logs
```

#### Argo UI returns 503

The Argo ingress enables nginx basic-auth against a secret named `argo-basic-auth` in namespace `argo` — and **no script in this repo creates it**, so the UI stays broken until you do it once by hand:

```bash
sudo apt install -y apache2-utils
htpasswd -cB auth <username>
kubectl create secret generic argo-basic-auth --from-file=auth -n argo
rm auth
```

Past basic-auth, the UI still asks for a bearer token because argo-server runs in `client` auth mode:

```bash
./manage.sh
# Choose: 6) Get Argo token  → paste the whole "Bearer ..." string into the UI
```

#### A console does not load (502 / 503 / cert warning)

```bash
# Shared causes — check these first for any of the five consoles
kubectl get ingress -A                  # is the host right? did manage.sh → 9 substitute it?
kubectl get certificates -A             # READY=False → see the certificate section above
kubectl get endpoints -n <namespace> <service>   # empty = no healthy backend pod
```

Per-console specifics:

```bash
# Harbor — push fails even though the UI works
#   → externalURL in harbor-values.yaml must equal the real REGISTRY_DOMAIN
# RabbitMQ — login rejected after step 12 (password rotation)
kubectl get secret -n db rabbitmq -o jsonpath='{.data.rabbitmq-password}' | base64 -d
# Grafana / Rancher — the service name must match the Helm release name
kubectl get svc -n monitoring prometheus-grafana
kubectl get svc -n cattle-system rancher
# Rancher redirect loop → the Rancher chart creates its own ingress for the same host
# as rancher-ingress.yaml; check for a duplicate and delete one
kubectl get ingress -n cattle-system
# Grafana loads but shows no data → check the datasources
kubectl logs -n monitoring deploy/prometheus-grafana -c grafana
```

### 9.5 End-to-end smoke test

Infrastructure being healthy does not prove the platform works. Verify the core feature explicitly:

```bash
# 1. Log into the admin panel (ADMIN_DOMAIN) and create + start a challenge
# 2. Watch the workflow that gets triggered
kubectl get wf -n argo -w

# 3. The challenge pod should appear and become Running
kubectl get pods -A -w

# 4. Log into the contestant portal (CONTESTANT_DOMAIN) and connect to the
#    challenge through GATEWAY_DOMAIN, then submit the flag
```

> [!NOTE]
> `./manage.sh → 6` (Get Argo token) prints a short-lived (1h) bearer token for calling the Argo API by hand while debugging. It is not a required setup step — the services read their own projected ServiceAccount token at runtime.

---

## 10. Estimated Cost & Optimization

### Monthly cost (estimated, region asia-southeast1)

| Resource | Spec | Cost/month (USD) |
|---|---|---|
| VM Master | e2-standard-8 (8 vCPU, 32GB) | ~$195 |
| VM Worker | e2-highmem-8 (8 vCPU, 64GB) | ~$260 |
| Disk Master | 200GB SSD | ~$34 |
| Disk Worker | 200GB SSD | ~$34 |
| Static IP | 1 IP | ~$7 |
| Network Egress | ~50GB/month | ~$6 |
| **Total** | | **~$536/month** |

### How to save

| Method | Savings | Notes |
|---|---|---|
| Use `e2-standard-4` for both VMs | ~50% | Enough for a small test/demo |
| Use **Spot VMs** | ~60-90% | The VM can be reclaimed |
| **Committed Use** (1 year) | ~37% | Requires a usage commitment |
| Stop VMs when not in use | Varies | `gcloud compute instances stop` |
| Use **Standard Disk** instead of SSD | ~40% of disk cost | Slower I/O |

### Stopping/Starting VMs to save money

```powershell
# Stop when not in use (data is NOT lost)
gcloud compute instances stop fctf-master fctf-worker --zone=asia-southeast1-b

# Start again when needed
gcloud compute instances start fctf-master fctf-worker --zone=asia-southeast1-b

# After starting again, SSH into the master and check:
# K3s restarts automatically, but it may take 2-3 minutes for all pods to become healthy
```

> [!IMPORTANT]
> When a VM is stopped, you are **not charged for the VM** (only for the disk and static IP).
> This is the most effective way to save money if you only use it a few hours a day.

---

## 11. Cleanup / Deleting Resources

Two separate layers: the platform inside the VMs, then the GCP infrastructure itself. If you are throwing the VMs away anyway, 11.1 is optional — but run it if you want to reuse the VMs, or if you need the platform gone while keeping the infrastructure.

### 11.1 Uninstall the FCTF platform

```bash
# On the WORKER first
./manage.sh
# Choose: 8) Uninstall → 1) Uninstall worker

# Then on the MASTER
./manage.sh
# Choose: 8) Uninstall → 2) Uninstall master
```

Order matters: removing the master first leaves the worker orphaned with no API server to deregister from.

| Script | What it removes |
|---|---|
| `uninstall-worker.sh` | Stops/disables `k3s-agent`, removes all CRI containers, unmounts kubelet/rancher mounts, runs `k3s-agent-uninstall.sh`, deletes `/var/lib/rancher`, `/var/lib/kubelet`, `/etc/cni`, containerd data, gVisor, and the Calico/K3s virtual interfaces |
| `uninstall.sh` (master) | `helm uninstall` for all 11 releases → force-deletes PVs (patches finalizers) → `clean-nfs.sh` → purges NFS packages → stops K3s → prunes and **uninstalls Docker incl. `/var/lib/docker`** → removes gVisor → runs `k3s-uninstall.sh` → deletes `/etc/rancher`, `/var/lib/rancher`, `/var/lib/kubelet`, `/var/lib/cni`, `/opt/cni`, `/var/lib/containerd`, `~/.kube` |

> [!CAUTION]
> The master script is a **full node teardown, not an app-level uninstall.** It destroys the cluster and all data:
> - `clean-nfs.sh` runs `rm -rf /srv/nfs/share` — every uploaded challenge file and app volume. It does back up `/etc/exports` first, and only removes its own export line.
> - Harbor's PVCs (~114Gi of pushed images) go with the force-deleted PVs, and `/var/lib/docker` takes the local build cache with it.
> - There is **no** app-only uninstall script in the repo. To reset just the platform and keep the cluster, delete the app namespaces and re-run `./manage.sh → 3` instead — verify your PVs' reclaim policy first, since NFS data is reused.
>
> Back up anything you need before running it (see the disk snapshot command in 11.2).

> [!NOTE]
> Both scripts are best-effort by design: they ignore individual command failures and print `[WARN]`, so a clean exit does not prove everything was removed. The master script skips namespace deletion entirely (that block is commented out), which is harmless once K3s is gone. Verify with:
> ```bash
> systemctl status k3s k3s-agent   # both should be gone
> ls /srv/nfs/share                # should not exist
> ls /var/lib/rancher /etc/rancher # should not exist
> ```

`uninstall.sh` accepts `--yes` to skip the confirmation prompt (it otherwise prints the current kube-context and asks).

### 11.2 Delete the GCP infrastructure

When you no longer need it, **DELETE EVERYTHING** to avoid charges:

```powershell
# 1. Delete the VMs
gcloud compute instances delete fctf-master fctf-worker `
  --zone=asia-southeast1-b --quiet

# 2. Delete the Static IP
gcloud compute addresses delete fctf-master-ip `
  --region=asia-southeast1 --quiet

# 3. Delete the Firewall rules
gcloud compute firewall-rules delete `
  fctf-allow-ssh `
  fctf-allow-http-https `
  fctf-allow-k3s-api `
  fctf-allow-internal `
  fctf-allow-nodeports --quiet

# 4. Delete the Subnet & VPC
gcloud compute networks subnets delete fctf-subnet `
  --region=asia-southeast1 --quiet
gcloud compute networks delete fctf-vpc --quiet

# 5. (Optional) Delete the DNS zone
gcloud dns managed-zones delete fctf-zone --quiet

# 6. Verify no resources remain
gcloud compute instances list
gcloud compute addresses list
gcloud compute firewall-rules list --filter="network:fctf-vpc"
```

> [!CAUTION]
> **Deleting a VM also deletes its disk and all data.** If you want to keep the data, create a snapshot first:
> ```powershell
> gcloud compute disks snapshot fctf-master `
>   --zone=asia-southeast1-b `
>   --snapshot-names=fctf-master-backup
> ```

---

## Quick Reference: End-to-End Command Flow

```mermaid
graph TD
    A["Windows Machine<br/>(gcloud CLI)"] -->|"gcloud compute ssh"| B["fctf-master VM"]
    A -->|"gcloud compute ssh"| C["fctf-worker VM"]

    B -->|"1. ./manage.sh → 9"| D["Configure Domains/IP"]
    D -->|"2. Edit PVs, Secrets, CMs"| E["Update Config Files"]
    E -->|"3. ./manage.sh → 1"| F["Setup K3s Master"]

    F -->|"4. Get Token"| G["Master Token"]
    G -->|"5. SSH to worker"| C
    C -->|"6. ./manage.sh → 2"| H["Join Worker to Cluster"]

    H -->|"7. Back to Master"| B
    B -->|"8. ./manage.sh → 3"| I["Install FCTF"]
    I -->|"9. ./manage.sh → 4"| J["Setup Harbor"]
    J -->|"10. Configure DNS"| K["DNS A Records"]
    K --> L["✅ FCTF Live!"]
```

### Quick Checklist

- [ ] Create GCP project + enable APIs
- [ ] Create VPC + Firewall rules
- [ ] Reserve Static IP
- [ ] Create Master VM + Worker VM
- [ ] SSH into Master → Clone repo
- [ ] `./manage.sh → 9` (Configure domains)
- [ ] Edit PV files (NFS server IP)
- [ ] Edit Secrets (passwords)
- [ ] Review ConfigMaps (URLs)
- [ ] `./manage.sh → 1` (Setup master)
- [ ] SSH into Worker → Clone repo
- [ ] `./manage.sh → 2` (Join cluster)
- [ ] Back to Master → `./manage.sh → 3` (Install FCTF)
- [ ] Configure DNS records
- [ ] Harbor UI: create the private project `fctf` + a robot account with pull/push
- [ ] `./manage.sh → 4` (Setup Harbor)
- [ ] `./manage.sh → 5` (Setup CI/CD) + add the GitHub secrets
- [ ] Verify all pods are Running
- [ ] Test web access

---

## Appendix

### A. `manage.sh` menu reference

| Option | Script | Run on | Notes |
|---|---|---|---|
| 1) Setup master | `FCTF-k3s-manifest/setup-master.sh` | master | Asks for the TLS SAN and the NFS allowed subnet |
| 2) Setup worker | `setup-worker.sh` | worker | Asks for the master URL + join token; installs gVisor by default |
| 3) Install FCTF | `apply-fctf.sh` | master | The 12-step deploy in 7.1 |
| 4) Setup harbor | `setup-harbor.sh` | master | Needs Docker + a Harbor robot account (7.3) |
| 5) Setup CI/CD | `cicd-setup.sh` | master | Prints the base64 kubeconfig for GitHub Actions (7.4) |
| 6) Get Argo token | `prod/sa/argo-workflow/get-token.sh` | master | 1h bearer token for the Argo UI/API |
| 7) Get master token | *(inline)* | master | `cat /var/lib/rancher/k3s/server/node-token` |
| 8) Uninstall | `uninstall/uninstall-worker.sh` / `uninstall.sh` | both | Worker first, then master (11.1) |
| 9) Configure service domains/IP | `configure-domains.sh` | master | Substitutes the 10 placeholder tokens |
| 0) Exit | — | — | — |

### B. Namespaces and what lives in them

| Namespace | Contents | Installed by |
|---|---|---|
| `app` | The 7 platform services + NetworkPolicies + `regcred` | `apply-fctf.sh` |
| `db` | MariaDB, Redis, RabbitMQ | Helm (`prod/helm.sh`) |
| `argo` | Argo Workflows server/controller, workflow templates, `global-regcred`, `docker-registry-creds` | Helm + `apply-fctf.sh` |
| `registry` | Harbor (core, portal, registry, jobservice, internal DB + Redis) | Helm |
| `monitoring` | Prometheus, Grafana, Loki | Helm |
| `cattle-system` | Rancher | Helm |
| `ingress-nginx` | ingress-nginx controller | Helm |
| `cert-manager` | cert-manager + the `letsencrypt-dev` ClusterIssuer | Helm + `apply-fctf.sh` |
| `storage` | NFS-backed PVs/PVCs | `apply-fctf.sh` |

The 7 services in `app`: `contestant-portal`, `contestant-be`, `admin-mvc`, `deployment-center`, `deployment-listener`, `deployment-consumer`, `challenge-gateway`.

### C. Default credentials — change all of these

Every value below is committed in the repo. `rotate-service-passwords.sh` (step 12 of `apply-fctf.sh`) rotates the service accounts but **deliberately leaves the four admin accounts alone**.

| What | File | Key | Rotated automatically? |
|---|---|---|---|
| Harbor admin | `prod/helm/registry/harbor-values.yaml` | `harborAdminPassword` | ❌ no |
| Harbor internal DB | same | `database.internal.password` | ✅ |
| Harbor registry user | same | `registry.credentials.password` | ✅ |
| Harbor secret key | same | `secretKey` | ✅ |
| RabbitMQ admin | `prod/helm/db/rabbitmq/rabbitmq-values.yaml` | `auth.password` (`rabbit-admin`) | ❌ no |
| RabbitMQ producer/consumer | `apply-fctf.sh` | `RABBIT_*_BOOTSTRAP_PASSWORD` | ✅ |
| Grafana admin | `prod/helm/monitoring/prometheus-stack-values.yaml` | `adminPassword` | ❌ no |
| Rancher bootstrap | `prod/helm/rancher/rancher-values.yaml` | `bootstrapPassword` | ❌ no |
| MariaDB | `prod/env/secret/mariadb-auth-secret.yaml` | — | ✅ |
| Redis | `prod/env/secret/redis-auth-secret.yaml`, `redis-acl-users-secret.yaml` | — | ✅ |
| App service secrets | `prod/env/secret/*-secret.yaml` (7 files) | — | partly — review each |
| ACME contact email | `prod/cert-manager/cluster-issuer.yaml` | `spec.acme.email` | ❌ no — currently a developer's address |

### D. Ports reference

| Port | Used by | Opened by (Phase 2) |
|---|---|---|
| 22 | SSH | `fctf-allow-ssh` |
| 80 / 443 | ingress-nginx (all web traffic, HTTP-01 challenges) | `fctf-allow-http-https` |
| 6443 | K3s API server | `fctf-allow-k3s-api` (internal only — see 7.4 for CI/CD) |
| 2049 + Calico VXLAN | NFS, pod networking | `fctf-allow-internal` |
| 30000-32767 | NodePort range | `fctf-allow-nodeports` |

In-cluster service ports (ClusterIP by default, reached through the ingress): `argo-workflows-server:2746`, `rabbitmq:15672`, `prometheus-grafana:80`, `harbor:80`, `rancher:443`.

> [!WARNING]
> `apply-fctf.sh` defaults to `--service-mode clusterip`, so no app NodePorts are created. If you pass `--service-mode nodeport`, [service-nodeport.yaml](FCTF-k3s-manifest/prod/app/service-nodeport.yaml) publishes `admin-mvc:30080`, `contestant-portal:30517`, `contestant-be:30501` — and the `fctf-allow-nodeports` rule opens 30000-32767 to `0.0.0.0/0`, putting them straight on the internet with no TLS. Worse, [db-nodeport.yaml](FCTF-k3s-manifest/prod/db-nodeport.yaml) (MariaDB `30306`, Redis `30320`) would do the same for your databases — it is **not** applied by any script; do not apply it on a public VM.

### E. Key file paths

| Path | Purpose |
|---|---|
| `manage.sh` | The menu wrapper for everything |
| `FCTF-k3s-manifest/apply-fctf.sh` | Main deploy script |
| `FCTF-k3s-manifest/prod/helm.sh` | All Helm releases |
| `FCTF-k3s-manifest/prod/env/secret/`, `env/configmap/` | Per-service secrets and config |
| `FCTF-k3s-manifest/prod/storage/pv/` | NFS PersistentVolumes (edit the server IP — section 6.3) |
| `FCTF-k3s-manifest/prod/ingress/nginx/`, `ingress/certificate/` | Ingress rules and Certificates |
| `FCTF-k3s-manifest/prod/cert-manager/cluster-issuer.yaml` | ACME issuer |
| `FCTF-k3s-manifest/prod/argo-workflows/` | Challenge build/start workflow templates |
| `.github/workflows/ci-cd.yml` | CI/CD pipeline (hardcoded registry domain — see 7.4) |

### F. Command cheat sheet

```bash
# Cluster
kubectl get nodes -o wide
kubectl get pods -A
kubectl get events -A --sort-by='.lastTimestamp' | tail -20

# A single service
kubectl -n app logs deploy/<service> -f
kubectl -n app describe pod <pod>
kubectl -n app rollout restart deployment <service>

# Ingress / TLS
kubectl get ingress -A
kubectl get certificates -A
kubectl describe certificate -n <ns> <name>

# Challenges
kubectl get wf -n argo
kubectl get pods -A | grep challenge

# Join token (master)
sudo cat /var/lib/rancher/k3s/server/node-token
```

```powershell
# GCP
gcloud compute instances list
gcloud compute instances stop  fctf-master fctf-worker --zone=asia-southeast1-b
gcloud compute instances start fctf-master fctf-worker --zone=asia-southeast1-b
gcloud compute ssh fctf-master --zone=asia-southeast1-b
gcloud compute firewall-rules list --filter="network:fctf-vpc"
```
