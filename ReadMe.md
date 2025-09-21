Argo CD, RabbitMQ, and KEDA Project Setup
This guide provides a step-by-step process for setting up a Kubernetes environment with Argo CD for GitOps, RabbitMQ for messaging, and KEDA for event-driven autoscaling. This setup is ideal for microservice architectures, where services need to scale dynamically based on workload.

Prerequisites
Before you start, make sure you have the following tools installed:

Minikube or a similar Kubernetes cluster

kubectl to interact with your cluster

Helm for package management

docker and docker compose to build your application images

1. Argo CD Installation
   First, we'll install Argo CD, a powerful tool for declarative GitOps. We will create a dedicated namespace for it and apply the necessary manifest files.

Bash

kubectl create namespace argocd
kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml
2. RabbitMQ Installation
   Next, we'll set up RabbitMQ as our message broker using a Helm chart. This is a quick and reliable way to deploy complex applications like RabbitMQ on Kubernetes.

Bash

helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo update

helm install my-rabbitmq bitnami/rabbitmq
Accessing the RabbitMQ Management Dashboard
To access the RabbitMQ management interface, you'll need to use kubectl port-forward to expose the service to your local machine.

Bash

kubectl port-forward svc/my-rabbitmq 15672
You can then open your web browser and go to http://localhost:15672.

Default Credentials:

Username: user

Password: You can find the password in a Kubernetes secret. This is a secure practice to avoid hardcoding sensitive information.

3. Building Application Images
   Before deploying our applications, we need to build their Docker images. This process is managed by docker-compose.

Bash

docker compose build
4. Deploying Services
   With our images ready, we can deploy our To-Do API and the Image Processor Worker Service to the cluster using kubectl apply.

Bash

kubectl apply -f todo.api.deplyment.yaml
kubectl apply -f image.processor.workerservice.deployment.yaml
5. KEDA Setup for Autoscaling
   KEDA (Kubernetes Event-driven Autoscaling) allows our worker service to scale automatically based on the number of messages in the RabbitMQ queue. This prevents resource waste when there is no work and ensures your application can handle high loads when needed.

KEDA Installation
Install KEDA using its official Helm chart into its own namespace.

Bash

helm repo add kedacore https://kedacore.github.io/charts
helm repo update

helm install keda kedacore/keda --namespace keda --create-namespace
KEDA Configuration
Finally, we apply a secret for RabbitMQ credentials and a ScaledObject to tell KEDA how to scale our worker service. This ScaledObject defines the target deployment, the trigger (our RabbitMQ queue), and the scaling rules.

Bash

kubectl apply -f  rabbitmq-secret.yaml
kubectl apply -f  keda-rabbitmq-auth.yaml
kubectl apply -f  worker-scaledobject.yaml
This completes the full setup, providing a robust, scalable, and GitOps-ready environment for your projects.