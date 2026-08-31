# From Docker to Kubernetes on Azure AKS
### A Beginner's Guide, Built from the Ground Up

This document walks the full journey — from writing a single `Dockerfile` on your laptop all the way to running many copies of your app in production on Azure Kubernetes Service (AKS). It is written to build one idea on top of the next, so each section assumes only what came before it.

The mental models (apartment complex, company phone number, restaurant manager) are kept throughout, because they are the fastest way to *keep* these ideas straight once you've learned them.

---

## Table of Contents

1. [The Foundation: Docker](#1-the-foundation-docker)
2. [The Problem Docker Leaves Open](#2-the-problem-docker-leaves-open)
3. [Enter Kubernetes](#3-enter-kubernetes)
4. [The Kubernetes Hierarchy](#4-the-kubernetes-hierarchy)
5. [Services: The Stable Front Door](#5-services-the-stable-front-door)
6. [Local vs Production](#6-local-vs-production)
7. [Creating Kubernetes: Two Different Things](#7-creating-kubernetes-two-different-things)
8. [Hands-On: Docker → AKS, End to End](#8-hands-on-docker--aks-end-to-end)
9. [Quick Reference](#9-quick-reference)
10. [The One-Line Mental Models](#10-the-one-line-mental-models)

---

## 1. The Foundation: Docker

Before Kubernetes makes any sense, Docker has to be crisp. It helps to see that Docker is really **three separate things**, not one.

### 1.1 The three things in Docker

| Thing | What it is | Analogy |
|-------|-----------|---------|
| **Dockerfile** | The script/description you write: *"start from Ubuntu, install Python, copy my code in."* | A **class** — a blueprint |
| **Image** | The built, frozen result of that description. It doesn't run; it just sits there ready. | A **compiled blueprint** |
| **Container** | A running instance of an image. This is your live app. | An **object** — an instance of the class |

So the original intuition — *"Docker is a script that creates an object based on our description"* — is correct, with one refinement: the script is the **Dockerfile**, and the object is the **container**. The **image** is the frozen thing in between.

### 1.2 The flow

```
Dockerfile  ──build──►  Image  ──run──►  Container
(description)          (frozen)         (running app)
```

You write a description once, build it into an image once, and can then run that same image into **many** identical containers. That "one image, many containers" idea is the seed that Kubernetes later grows into something huge.

### 1.3 Writing a Dockerfile (example)

```dockerfile
# The description: what my app needs to run
FROM python:3.12-slim          # start from a base image
WORKDIR /app                   # set the working folder inside the container
COPY requirements.txt .        # copy dependency list in
RUN pip install -r requirements.txt   # install dependencies
COPY . .                       # copy the rest of the app in
EXPOSE 8080                    # the port the app listens on
CMD ["python", "app.py"]       # the command that starts the app
```

### 1.4 Building and running it

```bash
# Build the description into an image
docker build -t my-app:1.0 .

# Run the image into a live container
docker run -p 8080:8080 my-app:1.0
```

At this point you have exactly **one container running on one machine** (your laptop). That's the entire scope of Docker on its own — and it's also exactly where the trouble begins.

---

## 2. The Problem Docker Leaves Open

One container on your laptop is easy. Production is not.

In the real world you might have **dozens or hundreds** of containers spread across **many servers**. Suddenly you're babysitting them by hand:

- A container **crashes at 3 a.m.** — someone has to wake up and restart it.
- **Traffic doubles** — someone has to manually launch more copies.
- **A whole server dies** — its containers need to be moved to healthy machines.
- Containers need to **find each other**, get **updated without downtime**, and share load.

Docker gives you the *container*. It does **not** give you a way to run and manage a whole fleet of them automatically. That missing piece is what the next section fills.

---

## 3. Enter Kubernetes

**Kubernetes** (almost always written **"K8s"** — a *K*, eight letters, an *s*) is the tool that does all that babysitting for you, automatically, across a whole fleet of machines.

The category name for such a tool is a **container orchestrator**. If Docker is the musician playing one instrument, Kubernetes is the conductor coordinating the whole orchestra.

### 3.1 The key idea: declarative, one level up

Here is the single most important concept, and it connects directly to how Docker already works:

> Docker is **declarative** — you *describe* an image.
> Kubernetes is **declarative too**, just one level higher — you *describe the desired state of your whole system.*

Instead of describing one image, you tell Kubernetes something like:

> *"I want **3 copies** of this container running, reachable at **this address**."*

Then Kubernetes runs a constant loop in the background:

```
        ┌─────────────────────────────────────────┐
        │   Compare DESIRED state  vs  ACTUAL state │
        │              ▲                    │        │
        │              │                    ▼        │
        │        (your YAML)          (what's real)  │
        │              │                    │        │
        │              └──── fix any ───────┘        │
        │                   difference                │
        └─────────────────────────────────────────┘
                        (repeats forever)
```

This is called the **reconciliation loop**, and it gives you **self-healing** for free. If a container dies, Kubernetes notices *"I should have 3, I only see 2"* and starts a replacement — with **no human involved.** That loop is the heart of everything Kubernetes does.

### 3.2 The restaurant manager analogy

You don't tell a restaurant manager *"go hire Bob, now hire Alice."* You say *"keep 3 people staffed at this station,"* and the manager handles sick days, busy nights, and reassignments to keep that true.

**Kubernetes is that manager for your containers.** You state the goal; it does the ongoing work of keeping reality matched to the goal.

---

## 4. The Kubernetes Hierarchy

This section answers the questions: *How is a pod different from a cluster? Can one node have many pods?*

The short answer: these things aren't competing alternatives — they're **nested inside each other**, like a set of Russian dolls, from biggest to smallest.

### 4.1 The four nested layers

```
┌─────────────────────────────────────────────────────┐
│ CLUSTER  (the whole group of machines)               │
│                                                       │
│   ┌───────────────────┐   ┌───────────────────┐      │
│   │ NODE (machine 1)  │   │ NODE (machine 2)  │      │
│   │                   │   │                   │      │
│   │  ┌─────┐ ┌─────┐  │   │  ┌─────┐          │      │
│   │  │ POD │ │ POD │  │   │  │ POD │          │      │
│   │  │┌───┐│ │┌───┐│  │   │  │┌───┐│          │      │
│   │  ││ C ││ ││ C ││  │   │  ││ C ││          │      │
│   │  │└───┘│ │└───┘│  │   │  │└───┘│          │      │
│   │  └─────┘ └─────┘  │   │  └─────┘          │      │
│   └───────────────────┘   └───────────────────┘      │
│                                                       │
└─────────────────────────────────────────────────────┘
     C = Container
```

- **Cluster** — the whole group of machines Kubernetes runs on. *(biggest box)*
- **Node** — one machine inside the cluster. *(can be a physical server or a virtual machine)*
- **Pod** — the smallest unit Kubernetes runs, sitting on a node. Usually a thin wrapper around **one** container.
- **Container** — your actual running app, inside the pod. *(smallest box)*

So: a cluster **contains** nodes, a node **contains** pods, and a pod **contains** a container. They are different *sizes on the same ladder*, not different choices.

### 4.2 The apartment complex analogy

This is the analogy to remember. It maps perfectly:

| Kubernetes term | Apartment analogy |
|-----------------|-------------------|
| **Cluster** | The whole apartment complex |
| **Node** | One building in the complex |
| **Pod** | One apartment inside a building |
| **Container** | The person living in the apartment |

One building obviously has **many apartments**, and each apartment usually has **one resident** (one container per pod is the normal case). That single picture answers most "how do these fit together" questions.

### 4.3 Can one node run many pods?

**Yes — absolutely.** One node can run many pods at the same time, limited only by how much **CPU and memory** that machine has. (One building holds many apartments.)

Kubernetes decides *which pods land on which nodes* automatically. That placement step has a name: **scheduling**. You never have to say "put this pod on machine 2" — the scheduler works it out based on available resources.

### 4.4 A note on "one container per pod"

The normal case is **one container per pod**, and that's the right assumption while you're learning. A pod *can* hold more than one container when they're tightly coupled and must always live together (a helper "sidecar" container, for example) — but you can safely ignore that until you need it.

---

## 5. Services: The Stable Front Door

This section answers: *What do you mean by a Service? Is it something other than a machine?*

**Yes — a Service is not a machine and not hardware.** It's a **networking idea**. To see why it exists, you first have to see the problem it solves.

### 5.1 The problem: pods are disposable

Every pod gets its own IP address. But pods are **disposable** — when one dies and Kubernetes replaces it (remember the self-healing loop), the new pod comes back with a **different IP address**.

So if some other app had memorized *"talk to the pod at 10.1.2.3"*, it **breaks the instant that pod is replaced.** Chasing constantly-changing IP addresses is not workable.

### 5.2 What a Service is

A **Service** is a **stable front door** — a fixed name and address that always routes to whatever pods are currently alive behind it, even as those pods come and go.

It is a **virtual routing rule** that Kubernetes maintains for you. Not a server, not a machine — just a stable address plus the logic to forward traffic to healthy pods.

### 5.3 The company phone number analogy

The cleanest way to picture it: a company's **main phone number**.

You always dial the **same number**, and it connects you to whoever happens to be available on, say, the sales team. Individual salespeople get hired, quit, or go on vacation — but **the number never changes.**

The Service **is** that phone number for your pods. Callers use one stable address; Kubernetes quietly connects them to whichever pods are up right now.

### 5.4 (Optional) Common Service types

You'll eventually meet a few flavors. Just know they exist:

| Type | Reaches your pods from... |
|------|---------------------------|
| **ClusterIP** | *inside* the cluster only (the default) |
| **NodePort** | a fixed port on every node |
| **LoadBalancer** | *outside* the cluster, via a cloud load balancer (used in the AKS example below) |

---

## 6. Local vs Production

This is the framing that ties Docker and Kubernetes together, and it's worth stating plainly.

### 6.1 On your laptop (local)

You run **one container on one machine** with `docker run`. If it crashes, *you* restart it. If you need a second copy, *you* start it by hand. That's completely fine for development — the manual work is trivial because there's so little of it.

### 6.2 In production

You have **many containers across many machines**, and the manual work explodes: restarts, scaling, moving workloads off dead servers, wiring services together. Doing this by hand is impossible at scale. This is precisely the job you hand to **Kubernetes**.

### 6.3 Side by side

| | Local (development) | Production |
|---|---|---|
| **Scale** | One container, one machine | Many containers, many machines |
| **When something crashes** | You restart it manually | Kubernetes restarts it automatically |
| **Scaling up** | You start more copies by hand | Kubernetes launches copies to match your desired count |
| **A machine dies** | Not really a concern | Kubernetes reschedules its pods onto healthy nodes |
| **Typical tool** | Docker (or a tiny local K8s like *minikube* / *kind*) | A managed cluster like **Azure AKS** |

The mental leap: **local is the manual version; production with Kubernetes is the same idea, automated.**

---

## 7. Creating Kubernetes: Two Different Things

This section answers: *How do we create Kubernetes? Is there a script? Is it a unit?*

There are **two completely different things** hiding inside that question. Separating them clears up almost all the confusion.

### 7.1 Creating the cluster itself (the machines)

You generally **don't** build a cluster by hand — assembling the machines and installing Kubernetes on them is famously painful. Instead you use a **managed service**, and this is exactly where **Azure AKS** fits.

You *ask Azure* for a cluster, and Azure provisions the machines and installs Kubernetes on them for you. One command:

```bash
az aks create \
  --resource-group myGroup \
  --name myCluster \
  --node-count 3
```

That gives you a cluster with **3 nodes**, Kubernetes already set up. So creating the cluster isn't a script *you* write — it's a request you make to the cloud.

### 7.2 Creating things *inside* the cluster (the "scripts")

This is where the *"is there a script / is it a unit?"* instinct is exactly right.

To create things **inside** the cluster, you write **YAML files** called **manifests**. A manifest is a **unit of description** — it declares *what you want*, and it's the direct equivalent of your Dockerfile, just one level up (describing the running system instead of a single image).

You then hand these manifests to the cluster, and Kubernetes makes them real. The details of writing and applying them are in the next section.

> **In one line:** AKS builds the *cluster* for you with a command; YAML manifests are the *"scripts"* that declare what runs inside it.

---

## 8. Hands-On: Docker → AKS, End to End

This is the full pipeline, from a `Dockerfile` on your laptop to a running, publicly reachable app on AKS. Each step builds on the last.

### Step 1 — Write the Dockerfile

Describe the app (same as [Section 1.3](#13-writing-a-dockerfile-example)):

```dockerfile
FROM python:3.12-slim
WORKDIR /app
COPY requirements.txt .
RUN pip install -r requirements.txt
COPY . .
EXPOSE 8080
CMD ["python", "app.py"]
```

### Step 2 — Build the image and push it to a registry

The cluster's machines can't see your laptop, so the image must live somewhere they can pull it from. On Azure that's **Azure Container Registry (ACR)** — think of it as a shared shelf for images.

```bash
# Build the image
docker build -t my-app:1.0 .

# Tag it for your Azure registry
docker tag my-app:1.0 myregistry.azurecr.io/my-app:1.0

# Log in and push it up to ACR
az acr login --name myregistry
docker push myregistry.azurecr.io/my-app:1.0
```

### Step 3 — Create the AKS cluster

Ask Azure for the machines (from [Section 7.1](#71-creating-the-cluster-itself-the-machines)):

```bash
az aks create \
  --resource-group myGroup \
  --name myCluster \
  --node-count 3 \
  --attach-acr myregistry      # let the cluster pull from your registry
```

### Step 4 — Point `kubectl` at the cluster

`kubectl` is the command-line tool you use to talk to any Kubernetes cluster. This step connects it to your new AKS cluster:

```bash
az aks get-credentials --resource-group myGroup --name myCluster

# Sanity check — list the nodes Azure created for you
kubectl get nodes
```

### Step 5 — Write the Deployment manifest

A **Deployment** is your written declaration of *"run N copies of this image."* This is the manifest that drives the self-healing loop.

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: my-app
spec:
  replicas: 3                    # ← the desired state: 3 copies
  selector:
    matchLabels:
      app: my-app
  template:
    metadata:
      labels:
        app: my-app
    spec:
      containers:
      - name: my-app
        image: myregistry.azurecr.io/my-app:1.0   # ← your Docker image!
        ports:
        - containerPort: 8080
```

Notice the `image:` line — it points straight at the Docker image you pushed in Step 2. This is the seam where everything you learned about Docker plugs directly into Kubernetes.

### Step 6 — Write the Service manifest

A **Service** gives those pods a stable address (from [Section 5](#5-services-the-stable-front-door)). Using `type: LoadBalancer` on AKS also gets you a public IP.

```yaml
apiVersion: v1
kind: Service
metadata:
  name: my-app-service
spec:
  selector:
    app: my-app          # routes to any pod labeled app: my-app
  ports:
  - port: 80
    targetPort: 8080
  type: LoadBalancer     # gives a public IP on AKS
```

### Step 7 — Apply the manifests

Hand both descriptions to the cluster:

```bash
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml
```

### Step 8 — Verify it's running

```bash
kubectl get pods       # should show 3 pods running
kubectl get service    # shows the public IP once it's assigned
```

### What actually happens when you apply

From the moment you run `kubectl apply`, Kubernetes takes over:

1. It reads your desired state: *3 pods of this image, reachable via this Service.*
2. The **scheduler** places those 3 pods onto nodes with room ([Section 4.3](#43-can-one-node-run-many-pods)).
3. Each node pulls the image from ACR and starts its container.
4. The Service starts routing traffic to those pods.
5. The **reconciliation loop** now runs forever: if a pod dies, Kubernetes sees *"I should have 3, I only see 2"* and launches a replacement — automatically.

That final loop is the whole payoff for describing your system declaratively instead of running commands by hand.

---

## 9. Quick Reference

### Concepts

| Term | One-sentence meaning |
|------|----------------------|
| **Dockerfile** | The description/script for building an image. |
| **Image** | The frozen, built result — like a class. |
| **Container** | A running instance of an image — like an object. |
| **Kubernetes (K8s)** | A container orchestrator that runs and manages many containers across many machines. |
| **Cluster** | The whole group of machines Kubernetes runs on. |
| **Node** | One machine in the cluster. |
| **Pod** | The smallest unit K8s runs; usually one container. |
| **Container (in K8s)** | Your app, running inside a pod. |
| **Deployment** | A manifest declaring "run N copies of this image." |
| **Service** | A stable address that routes to your pods as they come and go. |
| **Manifest (YAML)** | A file describing desired state — the "script" for the cluster. |
| **Scheduling** | K8s deciding which node each pod runs on. |
| **Reconciliation loop** | K8s continuously fixing any gap between desired and actual state. |
| **AKS** | Azure's managed Kubernetes — Azure builds and runs the cluster for you. |
| **ACR** | Azure Container Registry — where your images live so the cluster can pull them. |
| **kubectl** | The command-line tool for talking to a Kubernetes cluster. |

### Commands

| Command | What it does |
|---------|--------------|
| `docker build -t my-app:1.0 .` | Build an image from a Dockerfile. |
| `docker run -p 8080:8080 my-app:1.0` | Run one container locally. |
| `docker push myregistry.azurecr.io/my-app:1.0` | Push an image to ACR. |
| `az aks create ...` | Create an AKS cluster (the machines + Kubernetes). |
| `az aks get-credentials ...` | Point `kubectl` at your cluster. |
| `kubectl apply -f file.yaml` | Submit a manifest (make your description real). |
| `kubectl get pods` | List running pods. |
| `kubectl get service` | List services and their addresses. |
| `kubectl get nodes` | List the machines in the cluster. |

---

## 10. The One-Line Mental Models

Keep these five sentences and you keep the whole guide:

1. **Docker** packages and runs *one* container; **Kubernetes** runs and manages *many* containers across *many* machines, continuously keeping them in the state you asked for.
2. **Cluster → Node → Pod → Container** is one nested ladder — like a **complex → building → apartment → resident** — not a set of competing choices. One node holds many pods.
3. A **Service** is not a machine — it's a stable **company phone number** for your pods, so callers reach whoever's available even as pods come and go.
4. **Local** is the manual version (you restart, you scale); **production with Kubernetes** is the same idea, automated by the reconciliation loop.
5. **AKS** builds the *cluster* for you with a command; **YAML manifests** are the *scripts* that declare what runs inside it, and `kubectl apply` submits them.
