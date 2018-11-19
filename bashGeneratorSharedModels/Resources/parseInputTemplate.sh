# if command line tells us to parse an input file
if [ -z "${inputFile}" ]; then
     
     # we need to parse the input to get the inputFile
     parseInput "$@"
    
     # load parameters from the file
     inputConfig=$(jq . < "${inputFile}")
__FILE_TO_SETTINGS__
fi