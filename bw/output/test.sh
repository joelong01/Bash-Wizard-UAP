#!/bin/bash

#---------- see https://github.com/joelong01/starterBash ----------------
usage() {
    echo "Usage: $0  -f|--config-file -g|--log-directory" 1>&2 
    echo ""
    echo " -f | --config-file                    (Required)      the config file"
    echo " -g | --log-directory                  (Optional)      directory for the log file.  the log file name will be based on the script name"
    echo ""
    exit 1
}

echoInput() { 
    echo "scriptName.sh:"
    echo "    configFile                     $configFile"
    echo "    logFileDir                     $logFileDir"
}

# input variables 
declare configFile=.
declare logFileDir="./"

# make sure this version of *nix supports the right getopt 
! getopt --test > /dev/null
if [[ ${PIPESTATUS[0]} -ne 4 ]]; then
    echo "I'm sorry, 'getopt --test' failed in this environment."
    exit 1
fi

OPTIONS=f:g:
LONGOPTS=config-file:,log-directory:,
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
        -f|--config-file)
            configFile=$2
            shift 2
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

if [ -z "${configFile}" ]; then
    echo ""
    echo "Required parameter missing! "
    echoInput #make it easy to see what is missing
    echo ""
    usage
    exit 2
fi

declare LOG_FILE="${logFileDir}scriptName.sh.log"
mkdir "${logFileDir}" 2>> /dev/null
rm -f "${LOG_FILE}"  >> /dev/null
#creating a tee so that we capture all the output to the log file
{
time=$(date +"%m/%d/%y @ %r")
echo "started: $time"

echoInput


# --- YOUR SCRIPT STARTS HERE ---



# --- YOUR SCRIPT ENDS HERE ---
time=$(date +"%m/%d/%y @ %r")
echo "ended: $time"
} | tee -a "${LOG_FILE}"

hit any key to exit
