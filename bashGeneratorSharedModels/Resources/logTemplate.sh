#logging support
declare LOG_FILE="${logDirectory}__LOG_FILE_NAME__"
mkdir "${logDirectory}" 2>> /dev/null
rm -f "${LOG_FILE}"  >> /dev/null
