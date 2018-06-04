defmodule Sample do
    def main(args) do
        # Replace userToken with your producer token       
        userToken  =  "1/c97551d6bf784d1187f41d6f6c050267/9f05d6d2e914419898ec639a0657af61/365cc376-d621-41b0-b32f-7a982b22a63c/63671337180/5HLD7D%2FIbVui3BELRSVs8Pbc2%2F8X1%2Ba9X319biBXx8w%3D"       
        # Replace omfEndPoint with your OMF END POINT 
        omfEndPoint = "https://dat-a.osisoft.com/api/omf"
      
        typeBody = "[{
        \"id\": \"simple_data_type\",
        \"description\": \"A simple number + timestamp object\",
        \"version\": \"1.0.0.0\",
        \"type\": \"object\",
        \"classification\": \"dynamic\",
        \"properties\": {
                \"IndexedDateTimeProperty\": {
                        \"type\": \"string\",
                        \"isindex\": true
                },
                \"NumberProperty\": {
                        \"type\": \"number\",
                        \"format\": \"float64\"
                }
                     }
            }]"
           
        IO.inspect typeBody    

        IO.puts "******************************** Creating Simple type ***************************************************************"
        typeResponse = HTTPotion.post omfEndPoint, 
        [
            body: typeBody, 
            headers: 
                ["Accept": "application/json", 
                "Accept-Charset": "UTF-8" , 
                "Content-Type": "application/json;charset=UTF-8", 
                "producertoken": userToken, 
                "messageformat": "JSON", 
                "messagetype": "Type", 
                "omfversion": "1.0"
                ]
        ]

        IO.inspect typeResponse
        
        IO.puts "******************************** Simple Type Created ***************************************************************"

        IO.puts "******************************** Creating Simple container ***************************************************************"
       
        containerBody = "[{
        \"id\": \"simple_data_stream\",
        \"typeid\": \"simple_type\"
            }]"

        containerResponse = HTTPotion.post omfEndPoint, 
        [
            body: containerBody, 
            headers: 
                ["Accept": "application/json", 
                "Accept-Charset": "UTF-8" , 
                "Content-Type": "application/json;charset=UTF-8", 
                "producertoken": userToken, 
                "messageformat": "JSON", 
                "messagetype": "container", 
                "omfversion": "1.0"
                ]
        ]
      
        IO.puts "******************************** Simple Container Created ***************************************************************"
        IO.inspect containerResponse
        IO.puts "******************************** Sending the simple data ***************************************************************"

        #Method call to send data to SDS 
        omf_data(userToken, omfEndPoint)
      
        #For complex data, only type is generated but data is not sending.
        IO.puts "********************************Creating Complex Type **************************************************************"
        complexTypeBody = "[{
            \"id\": \"complex_type1\",
            \"description\": \"A complex type including strings, ints, doubles, arrays, and nested objects\",
            \"version\": \"1.0.0.0\",
            \"type\": \"object\",
            \"classification\": \"dynamic\",
            \"properties\": {
                    \"IndexedDateTimeProperty\": {
                            \"type\": \"string\",
                            \"format\": \"date-time\",
                            \"isindex\": true
                    },
                    \"StringProperty\": {
                            \"type\": \"string\"
                    },
                    \"DoubleProperty\": {
                        \"type\": \"number\"
                    },
                    \"IntegerProperty\": {
                        \"type\": \"integer\"
                    },  
                    \"ObjectProperty\": {
                        \"type\": \"object\",
                        \"properties\": 
                        {
                            \"NumberProperty\": {
                                \"type\": \"number\",
                                \"format\": \"float64\"
                            },  
                            \"IntegerArrayProperty\": 
                            {   \"type\": \"array\", 
                                \"items\": {
                                     \"type\": \"integer\" 
                                }, 
                                \"maxItems\": 3 
                            },
                            \"NestedObjectProperty\": 
                            {
                                \"type\": \"object\",
                                \"properties\": {
                                    \"NumberProperty\": {
                                        \"type\": \"number\",
                                        \"format\": \"float64\"
                                    },  
                                    \"StringProperty\": {
                                        \"type\": \"string\"
                                    }
                                } 
                            } 
                        }
                    }
                }
         }]"

        complexTypeResponse =  HTTPotion.post omfEndPoint, 
        [
            body: complexTypeBody, 
            headers: 
                ["Accept": "application/json", 
                "Accept-Charset": "UTF-8" , 
                "Content-Type": "application/json;charset=UTF-8", 
                "producertoken": userToken, 
                "messageformat": "JSON", 
                "messagetype": "Type", 
                "omfversion": "1.0"
                ]
        ]
        IO.inspect complexTypeResponse
        
        IO.puts "******************************** Complex Type created ***************************************************************"


    end

    #Method to send data to SDS 
    def omf_data(userToken, omfEndPoint) do
      
        Enum.each(0..99, fn(x) ->
        dateTime = Time.to_string(Time.utc_now())
        IO.inspect  " Inserting index  --  #{dateTime}"
        dataBody = "[{
        \"containerid\": \"simple_data_stream\",
        \"values\": [{
                  \"IndexedDateTimeProperty\": \"#{dateTime}\",
                   \"NumberProperty\": #{x}
         }]
        }]"

        dataResponse = HTTPotion.post omfEndPoint, 
        [
            body: dataBody, 
            headers: 
                ["Accept": "application/json", 
                "Accept-Charset": "UTF-8" , 
                "Content-Type": "application/json;charset=UTF-8", 
                "producertoken": userToken, 
                "messageformat": "JSON", 
                "messagetype": "data", 
                "omfversion": "1.0"
                ]
        ]

        IO.inspect dataResponse
        IO.puts "******************************** Simple data sent ***************************************************************"
        end)
    end
end