#!/bin/bash

function failed() {
    local error=${1:-Undefined error}
    echo "Failed: $error" >&2
    exit 1
}

function showUsage() {
    echo "Usage: $0 <'file-to-encode'> ['file-to-encode' ... ]>"
    exit 0
}

function addGitIgnore() {
   fileName=$1
   GITIGNORE=".gitignore"
   if ! [ -f "$GITIGNORE" ]; then
     echo "create $GITIGNORE for current directory"
     touch $GITIGNORE
   fi

   if ! grep -q "$fileName" "$GITIGNORE"; then
     echo "$fileName" >> "$GITIGNORE"
     echo "$fileName to $GITIGNORE"
   fi
}

addGitIgnore "*.b64"
addGitIgnore "*.b64.json"

# if empty then show usage and exit
if [ -z "$1" ]; then
  showUsage
fi

# iterate through all arguments to find the file to encode
for arg in "$@"
do
  # check if file exists
  if [ -f "$arg" ]; then
      # create '$arg.b64' file by encoding '$arg' file
      echo "encoding file '$arg' base64 to '$arg.b64'"
      base64 -i "$arg" -o - > "$arg".b64
      base64 -i "$arg".b64 --decode > "$arg".b64.json
  else
    failed "file '$arg' does not exist"
  fi
done
echo "done"
