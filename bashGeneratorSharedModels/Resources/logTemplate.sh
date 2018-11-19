#logging support
declare LOG_FILE="${logFileDir}__LOG_FILE_NAME__"
mkdir "${logFileDir}" 2>> /dev/null
rm -f "${LOG_FILE}"  >> /dev/null
