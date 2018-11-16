E:\GitHub\starterBash\bash-gen\bin\Release\netcoreapp2.1\publish\bash-gen.dll -f input.json
param: -f
#!/bin/bash

#---------- see https://github.com/joelong01/starterBash ----------------
usage() {
	echo "Usage: $0  -r|--resource-group -l|--location -n|--storage-account-name -d|--delete -f|--data-file -a|--add-extension -e|--alert-email -t|--add-alerts -g|--log-directory" 1>&2 
 	echo ""
	echo " -r | --resource-group                 (Required)      Azure Resource Group"
	echo " -l | --location                       (Required)      the location of the VMs"
	echo " -n | --storage-account-name           (Required)      Azure Storage Account that stores the telemetry."
	echo " -d | --delete                         (Optional)      deletes the Linux Azure Diagnostics Extension from all the VMs in --resource-group"
	echo " -f | --data-file                      (Required)      the file that contains that JSON file for the extension"
	echo " -a | --add-extension                  (Optional)      adds the Linux Azure Diagnostics Extension from all the VMs in --resource-group"
	echo " -e | --alert-email                    (Optional)      the email to send the alert to (optional if no alert is added)"
	echo " -t | --add-alerts                     (Optional)      adds the default alerts to all the VMs in --resource-group"
	echo " -g | --log-directory                  (Optional)      directory for the log file.  the log file name will be based on the script name"
	echo ""
	exit 1
}

echoInput() { 
	echo "addExtendedMonitoringToLinuxVms:"
	echo "	resourceGroup                  $resourceGroup"
	echo "	location                       $location"
	echo "	storageAccountName             $storageAccountName"
	echo "	deleteExtension                $deleteExtension"
	echo "	dataFile                       $dataFile"
	echo "	addExtension                   $addExtension"
	echo "	emailAlert                     $emailAlert"
	echo "	addAlerts                      $addAlerts"
	echo "	logFileDir                     $logFileDir"
}

# input variables 
declare resourceGroup=""
declare location=westus2
declare storageAccountName=""
declare deleteExtension="no"
declare dataFile=""
declare addExtension="no"
declare emailAlert=""
declare addAlerts="no"
declare logFileDir="./"

# make sure this version of *nix supports the right getopt 
! getopt --test > /dev/null
if [[ ${PIPESTATUS[0]} -ne 4 ]]; then
	echo "I'm sorry, 'getopt --test' failed in this environment."
	exit 1
fi

OPTIONS=r:l:n:df:ae:tg:
LONGOPTS=resource-group:,location:,storage-account-name:,delete,data-file:,add-extension,alert-email:,add-alerts,log-directory:,
# -use ! and PIPESTATUS to get exit code with errexit set
# -temporarily store output to be able to check for errors
# -activate quoting/enhanced mode (e.g. by writing out "--options")
# -pass arguments only via   -- "$@"   to separate them correctly
! PARSED=$(getopt --options=$OPTIONS --longoptions=$LONGOPTS --name "$0" -- "$@")
if [[ ${PIPESTATUS[0]} -ne 0 ]]; then
	# e.g. return value is 1
	# then getopt has complained about wrong arguments to stdout
	echo "you might be running bash on a Mac.  if so, run 'brew install gnu-getopt' to make the command line processing work."
	usage
	exit 2
fi

# read getopt's output this way to handle the quoting right:
eval set -- "$PARSED"
# now enjoy the options in order and nicely split until we see --
while true; do
	case "$1" in
		-r|--resource-group)
			resourceGroup=$2
			shift 2
		;;
		-l|--location)
			location=$2
			shift 2
		;;
		-n|--storage-account-name)
			storageAccountName=$2
			shift 2
		;;
		-d|--delete)
			deleteExtension="yes"
			shift 1
		;;
		-f|--data-file)
			dataFile=$2
			shift 2
		;;
		-a|--add-extension)
			addExtension="yes"
			shift 1
		;;
		-e|--alert-email)
			emailAlert=$2
			shift 2
		;;
		-t|--add-alerts)
			addAlerts="yes"
			shift 1
		;;
		-g|--log-directory)
			logFileDir=$2
			shift 2
		;;
		--)
			shift
			break
		;;
		*)
			echo "Invalid option $1 $2"
			exit 3
		;;
	esac
done

if [ -z "${resourceGroup}" ] || [ -z "${location}" ] || [ -z "${storageAccountName}" ] || [ -z "${dataFile}" ]; then
	echo ""
	echo "Required parameter missing! "
	echoInput #make it easy to see what is missing
	echo ""
	usage
	exit 2
fi

declare LOG_FILE="${logFileDir}addExtendedMonitoringToLinuxVms.log"
mkdir "${logFileDir}" 2>> /dev/null
rm -f "${LOG_FILE}"  >> /dev/null
#creating a tee so that we capture all the output to the log file
{
time=$(date +"%m/%d/%y @ %r")
echo "started: $time" >> "${LOG_FILE}"

echoInput


# --- YOUR SCRIPT STARTS HERE ---



# --- YOUR SCRIPT ENDS HERE ---
time=$(date +"%m/%d/%y @ %r")
echo "ended: $time" >> "${LOG_FILE}"
} | tee -a "${LOG_FILE}"
