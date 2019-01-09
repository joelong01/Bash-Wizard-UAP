#!/bin/bash
function echoError() {
    RED=$(tput setaf 1)
    NORMAL=$(tput sgr0)
    echo "${RED}${1}${NORMAL}"
}
function echoWarning() {
    YELLOW=$(tput setaf 3)
    NORMAL=$(tput sgr0)
    echo "${YELLOW}${1}${NORMAL}"
}
function echoInfo {
    GREEN=$(tput setaf 2)
    NORMAL=$(tput sgr0)
    echo "${GREEN}${1}${NORMAL}"
}
read -r -p "install .net core 2.2 for Ubuntu 16.04? [y/n]" response
if [ "$response" == 'Y' ] || [ "$response" == 'y' ]; then
    echoWarning "getting public key"
    wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb
    echoWarning "installing public key.  needs root access"
    sudo dpkg -i packages-microsoft-prod.deb
    echoWarning "installing apt https"
    sudo apt-get install apt-transport-https
    echoWarning "updating"
    sudo apt-get update
    echoWarning "installing .net sdk 2.2"
    sudo apt-get install dotnet-sdk-2.2
    echoWarning "looking for .net core..."
    if [[  -x "$(command -v dotnet)" ]]; then
        echoInfo "PASSED: .Net is now installed on this machine!"
        echo ""
    else
        echoError ".net not found in the path - something went wrong with the install."
        echoError "see https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x"
        echo ""
    fi
    
fi
