#!/bin/zsh

function failed() {
    local error=${1:-Undefined error}
    echo "Failed: $error" >&2
    exit 1
}

check_kubectl_installed() {
  if ! command -v kubectl &> /dev/null
  then
    failed "kubectl could not be found"
  fi
}

wait_for_seconds() {
  local seconds=$1
  local message=$2
  echo -n "$message "
  while [ $seconds -gt 0 ]; do
    sleep 1
    echo -n .
    seconds=$((seconds-1))
  done
  echo
}


########### Main ###########
check_kubectl_installed

if [ "$#" -ne 4 ]; then
  echo "Usage: $0 <container_name> <environment> <deploy_namespace> <kubeconfig_file_path>"
  echo "Example: $0 sparrow-cert DEV my-k8s-namespace ~/.kube/config"
  failed "number of arguments is incorrect"
fi
## arguments
CONTAINER_NAME=%1
ENVIRONMENT=%2
DEPLOY_NAMESPACE=%3
KUBECONFIG_FILE=%4

# Set yaml file prefix
if [ -z "$ENVIRONMENT" ]; then
  YML_FILE_PREFIX=$CONTAINER_NAME
else
  YML_FILE_PREFIX=$CONTAINER_NAME-$ENVIRONMENT
fi

# Add hostPath for all nodes in kube-system namespace to create {hostPath}/keystore/ directory
echo "Adding hostPath for all nodes"
kubectl apply -f $YML_FILE_PREFIX-daemonset.yml --kubeconfig $KUBECONFIG_FILE -n kube-system
wait_for_seconds 10 "Waiting for DaemonSet to be applied"

kubectl get pods -l app=create-cert-dir --kubeconfig $KUBECONFIG_FILE -n kube-system
echo "Deleting DaemonSet ..."
kubectl delete daemonset create-cert-dir --kubeconfig $KUBECONFIG_FILE -n kube-system

## Apply k8s resources
echo "Applying k8s resources for $CONTAINER_NAME in (env='$ENVIRONMENT')"
kubectl apply -f $YML_FILE_PREFIX-namespace.yml --kubeconfig $KUBECONFIG_FILE -n $DEPLOY_NAMESPACE
kubectl apply -f $YML_FILE_PREFIX-secret.yml --kubeconfig $KUBECONFIG_FILE -n $DEPLOY_NAMESPACE
kubectl apply -f $YML_FILE_PREFIX-network.yml --kubeconfig $KUBECONFIG_FILE -n $DEPLOY_NAMESPACE
wait_for_seconds 5 "Waiting for network policy to be applied"

## Apply CronJob
kubectl apply -f $YML_FILE_PREFIX-cronjob.yml --kubeconfig $KUBECONFIG_FILE -n $DEPLOY_NAMESPACE

## Check deployment status
echo "Checking deployment status for $CONTAINER_NAME in (env='$ENVIRONMENT')"
kubectl get pods --kubeconfig $KUBECONFIG_FILE -n $DEPLOY_NAMESPACE | grep $CONTAINER_NAME