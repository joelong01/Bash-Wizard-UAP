# we need to parse the input to get the inputFile
parseInput "$@"

# if command line tells us to parse an input file
if [ -z "${inputFile}" ]; then
    # load parameters from the file
    inputConfig=$(jq . < "${inputFile}")
__FILE_TO_SETTINGS__
fi