    function onVerify() {
        # --- USER CODE STARTS HERE ---
        __USER_VERIFY_CODE__
        # --- USER CODE ENDS HERE ---
    }
    function onDelete() {
        # --- USER CODE STARTS HERE ---
        __USER_DELETE_CODE__
        # --- USER CODE ENDS HERE ---
    }
    function onCreate() {
        # --- USER CODE STARTS HERE ---
        __USER_CREATE_CODE__
        # --- USER CODE ENDS HERE ---
    }
    
    # --- USER CODE STARTS HERE ---
    __USER_CODE_1__
    # --- USER CODE ENDS HERE ---

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

    # --- USER CODE STARTS HERE ---
    __USER_CODE_2__
    # --- USER CODE ENDS HERE ---
