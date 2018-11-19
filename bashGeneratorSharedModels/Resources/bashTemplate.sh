#!/bin/bash
#---------- see https://github.com/joelong01/Bash-Wizard----------------

# make sure this version of *nix supports the right getopt 
! getopt --test > /dev/null
if [[ ${PIPESTATUS[0]} -ne 4 ]]; then
    echo "I’m sorry, 'getopt --test' failed in this environment."
    exit 1
fi

usage() {
    
__USAGE_LINE__ 1>&2
__USAGE__  
    echo ""
    exit 1
}

echoInput() {     
	echo __ECHO__
}


function parseInput() {
    
    local OPTIONS=__SHORT_OPTIONS__
    local LONGOPTS=__LONG_OPTIONS__

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
    # read getopt’s output this way to handle the quoting right:
    eval set -- "$PARSED"
    # now enjoy the options in order and nicely split until we see --
    while true; do
        case "$1" in
__INPUT_CASE__
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
}

# input variables 
__INPUT_DECLARATION__

# now parse input to see if any of the parameters have been overridden
parseInput "$@"

__PARSE_INPUT_FILE




__REQUIRED_PARAMETERS__

__LOGGING_SUPPORT_

__BEGIN_TEE__

__ECHO_INPUT__


# --- END OF BASH WIZARD GENERATED CODE ---
