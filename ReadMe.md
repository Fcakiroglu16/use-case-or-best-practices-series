argo cd install

kubectl create namespace argocd
kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml

helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo update

helm install my-rabbitmq bitnami/rabbitmq

my-rabbitmq service port-forward  15672
default username : user
default password :you can show password in myrabbitmq secret

run command below for todo api and image processoer project
docker compose build

kubectl apply -f  todo.api.deplyment.yaml
kubectl apply -f image.processor.workerservice.deployment.yaml


Keda setup


helm repo add kedacore https://kedacore.github.io/charts

helm repo update

helm install keda kedacore/keda --namespace keda --create-namespace


kubectl apply -f  rabbitmq-secret.yaml
kubectl apply -f  keda-rabbitmq-auth.yaml

