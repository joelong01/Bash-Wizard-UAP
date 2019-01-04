#logging support
declare LOG_FILE="${logDirectory}__LOG_FILE_NAME__"
{
    mkdir -p "${logDirectory}" 
    rm -f "${LOG_FILE}"  
} 2>>/dev/null
#creating a tee so that we capture all the output to the log file
{
    time=$(date +"%m/%d/%y @ %r")
    echo "started: $time"