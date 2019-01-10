#!/bin/bash
# bashWizard version 0.901
#---------- see https://github.com/joelong01/Bash-Wizard----------------
# this will make the error text stand out in red - if you are looking at these errors/warnings in the log file
# you can use cat <logFile> to see the text in color.
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
# make sure this version of *nix supports the right getopt
! getopt --test 2>/dev/null
if [[ ${PIPESTATUS[0]} -ne 4 ]]; then
	echoError "'getopt --test' failed in this environment.  please install getopt."
    read -r -p "install getopt using brew? [y,n]" response
    if [[ $response == 'y' ]] || [[ $response == 'Y' ]]; then
        ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)" < /dev/null 2> /dev/null
        brew install gnu-getopt
        echo 'export PATH="/usr/local/opt/gnu-getopt/bin:$PATH"' >> ~/.bash_profile
        echoWarning "you'll need to restart the shell instance to load the new path"
    fi
   exit 1
fi
# we have a dependency on jq
if [[ ! -x "$(command -v jq)" ]]; then
	echoError "'jq is needed to run this script.  please install jq - see https://stedolan.github.io/jq/download/"
	exit 1
fi
function usage() {

    echoWarning "Parameters can be passed in the command line or in the input file.  The command line overrides the setting in the input file."

    echo ""
    echo ""
    echo "Usage: $0 -r|--resource-group -l|--datacenter-location -n|--instance-count -i|--vm-image -g|--generate-keys -u|--upgrade-policy -f|--input-file -o|--log-directory -c|--create -d|--delete -v|--verify -t|--root-name -a|--long-name " 1>&2
    echo ""
    echo " -r | --resource-group             Required    Azure Resource Group"
    echo " -l | --datacenter-location        Required    the location of the VMs"
    echo " -n | --instance-count             Required    The number of VMs in the Scale Set"
    echo " -i | --vm-image                   Required    The name of the operating system image as a URN alias, URN, custom image name or ID, or VHD blob URI. Value from: az vm image list, az vm image show"
    echo " -g | --generate-keys              Optional    Generate SSH public and private key files if missing. The keys will be stored in the ~/.ssh directory."
    echo " -u | --upgrade-policy             Required    The upgrade policy of the VM Scale Set"
    echo " -f | --input-file                 Optional    filename that contains the JSON values to drive the script.  command line overrides file"
    echo " -o | --log-directory              Optional    directory for the log file.  the log file name will be based on the script name"
    echo " -c | --create                     Optional    if set, creates the scale set"
    echo " -d | --delete                     Optional    if set, deletes the scale set"
    echo " -v | --verify                     Optional    checks to see if the deployed scale set matches the input parameters"
    echo " -t | --root-name                  Required    the root name used to generate names in this script"
    echo " -a | --long-name                  Required    this is a new param"  
    echo ""
    exit 1
}
function echoInput() {     
    echo "createVmScaleSet.sh:"
    echo -n "    resource-group......... "
    echoInfo "$resourceGroup"
    echo -n "    datacenter-location.... "
    echoInfo "$datacenterLocation"
    echo -n "    instance-count......... "
    echoInfo "$instanceCount"
    echo -n "    vm-image............... "
    echoInfo "$vmImage"
    echo -n "    generate-keys.......... "
    echoInfo "$generateKeys"
    echo -n "    upgrade-policy......... "
    echoInfo "$upgradePolicy"
    echo -n "    input-file............. "
    echoInfo "$inputFile"
    echo -n "    log-directory.......... "
    echoInfo "$logDirectory"
    echo -n "    create................. "
    echoInfo "$create"
    echo -n "    delete................. "
    echoInfo "$delete"
    echo -n "    verify................. "
    echoInfo "$verify"
    echo -n "    root-name.............. "
    echoInfo "$rootName"
    echo -n "    long-name.............. "
    echoInfo "$longName"

}

function parseInput() {
    
    local OPTIONS=r:l:n:i:gu:f:o:cdvt:a:
    local LONGOPTS=resource-group:,datacenter-location:,instance-count:,vm-image:,generate-keys,upgrade-policy:,input-file:,log-directory:,create,delete,verify,root-name:,long-name:

    # -use ! and PIPESTATUS to get exit code with errexit set
    # -temporarily store output to be able to check for errors
    # -activate quoting/enhanced mode (e.g. by writing out "--options")
    # -pass arguments only via   -- "$@"   to separate them correctly
    ! PARSED=$(getopt --options=$OPTIONS --longoptions=$LONGOPTS --name "$0" -- "$@")
    if [[ ${PIPESTATUS[0]} -ne 0 ]]; then
        # e.g. return value is 1
        # then getopt has complained about wrong arguments to stdout
        echoError "you might be running bash on a Mac.  if so, run 'brew install gnu-getopt' to make the command line processing work."
        usage
        exit 2
    fi
    # read getopt’s output this way to handle the quoting right:
    eval set -- "$PARSED"
    # now enjoy the options in order and nicely split until we see --
    while true; do
        case "$1" in
        -r | --resource-group)
            resourceGroup=$2
            shift 2
            ;;
        -l | --datacenter-location)
            datacenterLocation=$2
            shift 2
            ;;
        -n | --instance-count)
            instanceCount=$2
            shift 2
            ;;
        -i | --vm-image)
            vmImage=$2
            shift 2
            ;;
        -g | --generate-keys)
            generateKeys=true
            shift 1
            ;;
        -u | --upgrade-policy)
            upgradePolicy=$2
            shift 2
            ;;
        -f | --input-file)
            inputFile=$2
            shift 2
            ;;
        -o | --log-directory)
            logDirectory=$2
            shift 2
            ;;
        -c | --create)
            create=true
            shift 1
            ;;
        -d | --delete)
            delete=true
            shift 1
            ;;
        -v | --verify)
            verify=true
            shift 1
            ;;
        -t | --root-name)
            rootName=$2
            shift 2
            ;;
        -a | --long-name)
            longName=$2
            shift 2
            ;;
        --)
            shift
            break
            ;;
        *)
            echoError "Invalid option $1 $2"
            exit 3
            ;;
        esac
    done
}
# input variables 
declare resourceGroup=
declare datacenterLocation=
declare instanceCount=
declare vmImage=
declare generateKeys=false
declare upgradePolicy=
declare inputFile=
declare logDirectory="./"
declare create=false
declare delete=false
declare verify=false
declare rootName=
declare longName=

parseInput "$@"

# if command line tells us to parse an input file
if [ "${inputFile}" != "" ]; then
	# load parameters from the file
	configSection=$(jq . <"${inputFile}" | jq '."createVmScaleSet.sh"')
	if [[ -z $configSection ]]; then
		echoError "$inputFile or createVmScaleSet.sh section not found "
		exit 3
	fi
    resourceGroup=$(echo "${configSection}" | jq '.["resource-group"]' --raw-output)
    datacenterLocation=$(echo "${configSection}" | jq '.["datacenter-location"]' --raw-output)
    instanceCount=$(echo "${configSection}" | jq '.["instance-count"]' --raw-output)
    vmImage=$(echo "${configSection}" | jq '.["vm-image"]' --raw-output)
    generateKeys=$(echo "${configSection}" | jq '.["generate-keys"]' --raw-output)
    upgradePolicy=$(echo "${configSection}" | jq '.["upgrade-policy"]' --raw-output)
    logDirectory=$(echo "${configSection}" | jq '.["log-directory"]' --raw-output)
    create=$(echo "${configSection}" | jq '.["create"]' --raw-output)
    delete=$(echo "${configSection}" | jq '.["delete"]' --raw-output)
    verify=$(echo "${configSection}" | jq '.["verify"]' --raw-output)
    rootName=$(echo "${configSection}" | jq '.["root-name"]' --raw-output)
    longName=$(echo "${configSection}" | jq '.["long-name"]' --raw-output)

	# we need to parse the again to see if there are any overrides to what is in the config file
	parseInput "$@"
fi

#verify required parameters are set
if [ -z "${resourceGroup}" ] || [ -z "${datacenterLocation}" ] || [ -z "${instanceCount}" ] || [ -z "${vmImage}" ] || [ -z "${upgradePolicy}" ] || [ -z "${rootName}" ] || [ -z "${longName}" ]; then
	echo ""
	echoError "Required parameter missing! "
	echoInput #make it easy to see what is missing
	echo ""
	usage
	exit 2
fi

#logging support
declare LOG_FILE="${logDirectory}createVmScaleSet.sh.log"
{
    mkdir -p "${logDirectory}" 
    rm -f "${LOG_FILE}"  
} 2>>/dev/null
#creating a tee so that we capture all the output to the log file
{
    time=$(date +"%m/%d/%y @ %r")
    echo "started: $time"

    echoInput
    # --- BEGIN USER CODE ---
    function onVerify() {
        
    }
    function onDelete() {
        
    }
    function onCreate() {
        
    }
    
    
    
    

    #
    #   the order matters - delete, then create, then verify
    #

    if [[ $delete == "true" ]]; then
        onDelete
    fi

    if [[ $create == "true" ]]; then
        onCreate
    fi
   
    if [[ $verify == "true" ]]; then
        onVerify        
    fi

    

    # --- END USER CODE ---
    time=$(date +"%m/%d/%y @ %r")
    echo "ended: $time"
} | tee -a "${LOG_FILE}"

