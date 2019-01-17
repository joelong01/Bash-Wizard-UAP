{
  "ScriptName": "test.sh",
  "LoggingSupport": true,
  "AcceptsInputFile": true,
  "CreateVerifyDelete": true,
  "Version": "0.906",
  "Description": "Sample test script",
  "Parameters": [
    {
      "ShortParameter": "i",
      "LongParameter": "input-file",
      "Description": "filename that contains the JSON values to drive the script. command line overrides file",
      "VariableName": "inputFile",
      "RequiresInputString": true,
      "Default": "",
      "RequiredParameter": false,
      "ValueIfSet": "$2"
    },
    {
      "ShortParameter": "l",
      "LongParameter": "log-directory",
      "Description": "directory for the log file. the log file name will be based on the script name",
      "VariableName": "logDirectory",
      "RequiresInputString": true,
      "Default": "\"./\"",
      "RequiredParameter": false,
      "ValueIfSet": "$2"
    },
    {
      "ShortParameter": "v",
      "LongParameter": "verify",
      "Description": "verifies the script ran correctly",
      "VariableName": "verify",
      "RequiresInputString": false,
      "Default": "false",
      "RequiredParameter": false,
      "ValueIfSet": "true"
    },
    {
      "ShortParameter": "d",
      "LongParameter": "delete",
      "Description": "deletes whatever the script created",
      "VariableName": "delete",
      "RequiresInputString": false,
      "Default": "false",
      "RequiredParameter": false,
      "ValueIfSet": "true"
    },
    {
      "ShortParameter": "c",
      "LongParameter": "create",
      "Description": "creates the resource",
      "VariableName": "create",
      "RequiresInputString": false,
      "Default": "false",
      "RequiredParameter": false,
      "ValueIfSet": "true"
    },
    {
      "ShortParameter": "t",
      "LongParameter": "test",
      "Description": "a parameter that will be merged into example1.sh",
      "VariableName": "test",
      "RequiresInputString": false,
      "Default": "false",
      "RequiredParameter": false,
      "ValueIfSet": "true"
    }
  ]
}