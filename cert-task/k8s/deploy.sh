#!/bin/zsh

# Set environment variables for deployment
# Change these values to match your environment
CONTAINER_NAME=sparrow-cert
ENVIRONMENT=DEV
DEPLOY_NAMESPACE=melog-io-dev
KUBECONFIG_FILE=~/.kube/config-homelab

# Set yaml file prefix
if [ -z "$ENVIRONMENT" ]; then
  YML_FILE_PREFIX=$CONTAINER_NAME
else
  YML_FILE_PREFIX=$CONTAINER_NAME-$ENVIRONMENT
fi

# Add hostPath for all nodes in kube-system namespace to create {hostPath}/keystore/ directory
echo "Adding hostPath for all nodes"
kubectl apply -f $YML_FILE_PREFIX-daemonset.yml --kubeconfig $KUBECONFIG_FILE -n kube-system
echo -n "Waiting for daemonset to be applied "

count=5
while [ $count -gt 0 ]; do
  sleep 1
  echo -n .
  count=$((count-1))
done
echo

kubectl get pods -l app=create-cert-dir --kubeconfig $KUBECONFIG_FILE -n kube-system
echo "Deleting DaemonSet ..."
kubectl delete daemonset create-cert-dir --kubeconfig $KUBECONFIG_FILE -n kube-system

## Apply k8s resources
echo "Applying k8s resources for $CONTAINER_NAME in (env='$ENVIRONMENT')"
kubectl apply -f $YML_FILE_PREFIX-namespace.yml --kubeconfig $KUBECONFIG_FILE -n $DEPLOY_NAMESPACE
kubectl apply -f $YML_FILE_PREFIX-secret.yml --kubeconfig $KUBECONFIG_FILE -n $DEPLOY_NAMESPACE
kubectl apply -f $YML_FILE_PREFIX-deployment.yml --kubeconfig $KUBECONFIG_FILE -n $DEPLOY_NAMESPACE

## Check deployment status
echo "Checking deployment status for $CONTAINER_NAME in (env='$ENVIRONMENT')"
kubectl get pods --kubeconfig $KUBECONFIG_FILE -n $DEPLOY_NAMESPACE | grep $CONTAINER_NAME