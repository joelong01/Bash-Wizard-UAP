    time=$(date +"%m/%d/%y @ %r")
    echo "ended: $time"
} | tee -a "${LOG_FILE}"
