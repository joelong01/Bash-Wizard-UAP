    function onVerify() {
        
    }
    function onDelete() {
        
    }
    function onCreate() {
        
    }
    
    
    __USER_CODE_1__
    

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

    
