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
    read -p "install getopt using brew? [y,n]" response
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

    echo "The top level test script for verifying BashWizard"
    echo ""
    echo "Usage: $0 -i|--input-file -l|--log-directory -c|--create -v|--verify -d|--delete -t|--test-input-file -e|--test-logging -s|--test-create -r|--test-verify -x|--test-delete -o|--load-parse-save -p|--optional-test-parameter -b|--bw-dll " 1>&2
    echo ""
    echo " -i | --input-file                     Optional    filename that contains the JSON values to drive the script. command line overrides file"
    echo " -l | --log-directory                  Required    directory for the log file. the log file name will be based on the script name"
    echo " -c | --create                         Optional    creates the resource"
    echo " -v | --verify                         Optional    verifies the script ran correctly"
    echo " -d | --delete                         Optional    deletes whatever the script created"
    echo " -t | --test-input-file                Optional    calls the generated script passing in the --input-file parameter"
    echo " -e | --test-logging                   Optional    verifies that an input directory/file has been created and is working"
    echo " -s | --test-create                    Optional    tests the --create flag"
    echo " -r | --test-verify                    Optional    tests the --verify flag"
    echo " -x | --test-delete                    Optional    tests the --delete flag"
    echo " -o | --load-parse-save                Optional    parses this script and creates a new one"
    echo " -p | --optional-test-parameter        Optional    this parameter is used to test in the input file"
    echo " -b | --bw-dll                         Required    the full path to bw.dll command line tool for BashWizard"  
    echo ""
    exit 1
}
function echoInput() {     
    echo "testDriver.sh:"
    echo -n "    input-file................. "
    echoInfo "$inputFile"
    echo -n "    log-directory.............. "
    echoInfo "$logDirectory"
    echo -n "    create..................... "
    echoInfo "$create"
    echo -n "    verify..................... "
    echoInfo "$verify"
    echo -n "    delete..................... "
    echoInfo "$delete"
    echo -n "    test-input-file............ "
    echoInfo "$testInputFile"
    echo -n "    test-logging............... "
    echoInfo "$testLogging"
    echo -n "    test-create................ "
    echoInfo "$testCreate"
    echo -n "    test-verify................ "
    echoInfo "$testVerify"
    echo -n "    test-delete................ "
    echoInfo "$testDelete"
    echo -n "    load-parse-save............ "
    echoInfo "$loadParseSave"
    echo -n "    optional-test-parameter.... "
    echoInfo "$optionalTestParameter"
    echo -n "    bw-dll..................... "
    echoInfo "$bwDll"

}

function parseInput() {
    
    local OPTIONS=i:l:cvdtesrxop:b:
    local LONGOPTS=input-file:,log-directory:,create,verify,delete,test-input-file,test-logging,test-create,test-verify,test-delete,load-parse-save,optional-test-parameter:,bw-dll:

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
        -i | --input-file)
            inputFile=$2
            shift 2
            ;;
        -l | --log-directory)
            logDirectory=$2
            shift 2
            ;;
        -c | --create)
            create=true
            shift 1
            ;;
        -v | --verify)
            verify=true
            shift 1
            ;;
        -d | --delete)
            delete=true
            shift 1
            ;;
        -t | --test-input-file)
            testInputFile=true
            shift 1
            ;;
        -e | --test-logging)
            testLogging=true
            shift 1
            ;;
        -s | --test-create)
            testCreate=true
            shift 1
            ;;
        -r | --test-verify)
            testVerify=true
            shift 1
            ;;
        -x | --test-delete)
            testDelete=true
            shift 1
            ;;
        -o | --load-parse-save)
            loadParseSave=true
            shift 1
            ;;
        -p | --optional-test-parameter)
            optionalTestParameter=$2
            shift 2
            ;;
        -b | --bw-dll)
            bwDll=$2
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
declare inputFile=
declare logDirectory="./Logs"
declare create=false
declare verify=false
declare delete=false
declare testInputFile=false
declare testLogging=false
declare testCreate=false
declare testVerify=false
declare testDelete=false
declare loadParseSave=false
declare optionalTestParameter=""
declare bwDll=

parseInput "$@"

# if command line tells us to parse an input file
if [ "${inputFile}" != "" ]; then
	# load parameters from the file
	configSection=$(jq . <"${inputFile}" | jq '."testDriver.sh"')
	if [[ -z $configSection ]]; then
		echoError "$inputFile or testDriver.sh section not found "
		exit 3
	fi
    logDirectory=$(echo "${configSection}" | jq '.["log-directory"]' --raw-output)
    create=$(echo "${configSection}" | jq '.["create"]' --raw-output)
    verify=$(echo "${configSection}" | jq '.["verify"]' --raw-output)
    delete=$(echo "${configSection}" | jq '.["delete"]' --raw-output)
    testInputFile=$(echo "${configSection}" | jq '.["test-input-file"]' --raw-output)
    testLogging=$(echo "${configSection}" | jq '.["test-logging"]' --raw-output)
    testCreate=$(echo "${configSection}" | jq '.["test-create"]' --raw-output)
    testVerify=$(echo "${configSection}" | jq '.["test-verify"]' --raw-output)
    testDelete=$(echo "${configSection}" | jq '.["test-delete"]' --raw-output)
    loadParseSave=$(echo "${configSection}" | jq '.["load-parse-save"]' --raw-output)
    optionalTestParameter=$(echo "${configSection}" | jq '.["optional-test-parameter"]' --raw-output)
    bwDll=$(echo "${configSection}" | jq '.["bw-dll"]' --raw-output)

	# we need to parse the again to see if there are any overrides to what is in the config file
	parseInput "$@"
fi

#verify required parameters are set
if [ -z "${logDirectory}" ] || [ -z "${bwDll}" ]; then
	echo ""
	echoError "Required parameter missing! "
	echoInput #make it easy to see what is missing
	echo ""
	usage
	exit 2
fi

#logging support
declare LOG_FILE="${logDirectory}testDriver.sh.log"
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
# we have a dependency on .net core
	echo -n "looking for .net core..."
	if [[ ! -x "$(command -v dotnet)" ]]; then
		echoError "'.net core 2.1+ is needed to run this script.  please install it.  see https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x"
        echoError "if running Windows, this will likely work: curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel LTS"
        echoError "but you will have to edit ~./profile to add .net to your path"
		echoError "aftwards make sure dotnet.exe is in the path"
        echoError "if you are on a mac, you porbably want to add a symbolic link by running this: n -s /usr/local/share/dotnet/dotnet /usr/local/bin/"
		exit 1
	fi
	echoInfo "found it!"
	# first we use the BashWizard bw.dll to read this and parse this file and then create a new bash file
	# that we will run our tests against.

	declare newFileName="$0.2.sh"
	if [[ ! -f "$bwDll" ]]; then
		echoError "$bwDll does not exist. please pass a correct DLL"
        echoError "the DLL can either be built and published, or you can get it from https://github.com/joelong01/Bash-Wizard/Binaries"        
		exit 0
	else
		echoInfo "found $bwDll"
	fi

	# if we need to test verify,create, delete we load/parse/save this script
	if [[ $loadParseSave == true ]]; then
        echo "creating new script file $newFileName"
		dotnet $bwDll -p -i "$0" -o "$newFileName"
	else
		newFileName=$0
	fi

	echoInfo "Script: $0 has LogFile: $LOG_FILE"

	function onVerify() {
		echoInfo "onVerify called"
	}
	function onDelete() {
		echoInfo "onDelete called"
	}
	function onCreate() {
		echoInfo "onCreate called"
	}

	if [[ $testVerify == 'true' ]]; then
		echo "testing --verify"
		ret=$("$newFileName" --verify --log-directory ./logging/logs/)
		if [[ $ret == *"onVerify called"* ]]; then
			echoInfo "PASSED"
		else
			echoError "FAILED"
			echoError "ret is $ret"
		fi
	fi

	if [[ $testDelete == 'true' ]]; then
		echo "testing --delete"
		ret=$("$newFileName" --delete --log-directory ./logging/logs)
		if [[ $ret == *"onDelete called"* ]]; then
			echoInfo "PASSED"
		else
			echoError "FAILED"
			echoWarning "ret is:"
			echo "$ret"
		fi
	fi

	if [[ $testCreate == 'true' ]]; then
		echo "testing --create"
		ret=$("$newFileName" --create --log-directory ./logging/logs)
		if [[ $ret == *"onCreate called"* ]]; then
			echoInfo "PASSED"
		else
			echoError "FAILED"
			echoError "ret is $ret"
		fi
	fi

    if [[ $testInputFile == true ]]; then
        echo "Testing Input File"
        if [[ $optionalTestParameter == "Verify Test Parameter" ]]; then            
            echoInfo "PASSED"
        else
            echoError "FAILED: optional test parameter value: $optionalTestParameter.  Expected 'Verify Test Parameter"
        fi
    fi

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


	if [[ $loadParseSave == true ]]; then        
	    rm -f "$newFileName"
	fi
    # --- END USER CODE ---
    time=$(date +"%m/%d/%y @ %r")
    echo "ended: $time"
} | tee -a "${LOG_FILE}"

