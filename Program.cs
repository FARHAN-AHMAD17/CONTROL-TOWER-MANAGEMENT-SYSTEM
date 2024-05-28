using System; // Importing necessary namespaces
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace FLIGHT
{
    class Program
    {
        static void Main(string[] args)
        {
            const int port = 13000; // Port number for the server
            const string ipAddress = "127.0.0.1"; // IP address for the server
            try
            {
                // Create a TCP socket
                var clientSocket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);
                var ep = new IPEndPoint(IPAddress.Parse(ipAddress), port); // Create an endpoint with the given IP and port
                // Connect to the server
                clientSocket.Connect(ep);
                Console.WriteLine("Client is connected!"); // Log successful connection

                int flightNumber = 0;
                int airplaneId = 0;
                int fuelLevel = 0;

                // Get Flight Number from the user
                Console.WriteLine("Enter Flight Number (integer):");
                while (!int.TryParse(Console.ReadLine(), out flightNumber)) // Loop until valid integer input
                {
                    Console.WriteLine("Invalid input. Please enter a valid integer for Flight Number.");
                }

                // Get Airplane ID from the user
                Console.WriteLine("Enter Airplane ID (integer):");
                while (!int.TryParse(Console.ReadLine(), out airplaneId)) // Loop until valid integer input
                {
                    Console.WriteLine("Invalid input. Please enter a valid integer for Airplane ID.");
                }

                // Get Fuel Level from the user
                Console.WriteLine("Enter Fuel Level (integer):");
                while (!int.TryParse(Console.ReadLine(), out fuelLevel) || fuelLevel < 0) // Loop until valid non-negative integer input
                {
                    Console.WriteLine("Invalid input. Please enter a valid fuel level (non-negative integer).");
                }

                // Get initial Request Type from the user
                RequestType currentRequestType;
                int typeInput;
                while (true)
                {
                    Console.WriteLine("Enter Request Type: 1. Takeoff, 2. Land, 3. Emergency Landing");
                    if (!int.TryParse(Console.ReadLine(), out typeInput) || !Enum.IsDefined(typeof(RequestType), typeInput)) // Loop until valid enum input
                    {
                        Console.WriteLine("Invalid input. Please enter the valid option.");
                        continue;
                    }
                    currentRequestType = (RequestType)typeInput;
                    break; // Exit the loop after successful input
                }

                // Get Scheduled Arrival/Departure based on Request Type
                string scheduledArrival = "", scheduledDeparture = "", emergencyReason = "";
                switch (currentRequestType)
                {
                    case RequestType.Takeoff:
                        Console.WriteLine("Enter Scheduled Departure (YYYY-MM-DD HH:mm:ss):");
                        scheduledDeparture = Console.ReadLine(); // Read scheduled departure time
                        break;
                    case RequestType.Land:
                        Console.WriteLine("Enter Scheduled Arrival (YYYY-MM-DD HH:mm:ss):");
                        scheduledArrival = Console.ReadLine(); // Read scheduled arrival time
                        break;
                    case RequestType.EmergencyLanding:
                        Console.WriteLine("Enter Estimated Emergency Landing Time (YYYY-MM-DD HH:mm:ss):");
                        scheduledArrival = Console.ReadLine(); // Read estimated emergency landing time
                        Console.WriteLine("Enter Reason for Emergency Landing:");
                        emergencyReason = Console.ReadLine(); // Read reason for emergency landing
                        break;
                }

                // Construct Request Data
                var flightRequest = new FlightRequest
                {
                    RequestType = currentRequestType,
                    FlightNumber = flightNumber,
                    AirplaneId = airplaneId,
                    FuelLevel = fuelLevel,
                    ScheduledArrival = scheduledArrival,
                    ScheduledDeparture = scheduledDeparture,
                    EmergencyReason = emergencyReason
                };

                // Serialize the request data to JSON and send it to the server
                var jsonString = JsonSerializer.Serialize(flightRequest);
                var responseData = SendAndReceiveData(clientSocket, jsonString); // Send data and receive response
                Console.WriteLine("Control tower: " + responseData); // Log server response

                // Main loop for subsequent requests
                while (true)
                {
                    while (true)
                    {
                        Console.WriteLine("Enter Request Type: 1. Takeoff, 2. Land, 3. Emergency Landing");
                        if (!int.TryParse(Console.ReadLine(), out typeInput) || !Enum.IsDefined(typeof(RequestType), typeInput)) // Loop until valid enum input
                        {
                            Console.WriteLine("Invalid input. Please enter the valid option.");
                            continue;
                        }
                        var requestType = (RequestType)typeInput;

                        // Check for invalid request based on current state
                        if (currentRequestType == RequestType.Takeoff && requestType == RequestType.Takeoff)
                        {
                            Console.WriteLine("You have already requested to take off.");
                            Console.WriteLine("You must land or request an emergency landing to take off again.");
                            continue;
                        }
                        if (currentRequestType != RequestType.Takeoff && requestType != RequestType.Takeoff)
                        {
                            Console.WriteLine("You have already requested to land or emergency land.");
                            Console.WriteLine("You must take off to land or emergency land again.");
                            continue;
                        }

                        // Update currentRequestType based on the selected request
                        currentRequestType = requestType;
                        break; // Exit the loop after successful input
                    }

                    // Get Scheduled Arrival/Departure based on Request Type
                    scheduledArrival = "";
                    scheduledDeparture = "";
                    emergencyReason = "";
                    switch (currentRequestType)
                    {
                        case RequestType.Takeoff:
                            Console.WriteLine("Enter Scheduled Departure (YYYY-MM-DD HH:mm:ss):");
                            scheduledDeparture = Console.ReadLine(); // Read scheduled departure time
                            break;
                        case RequestType.Land:
                            Console.WriteLine("Enter Scheduled Arrival (YYYY-MM-DD HH:mm:ss):");
                            scheduledArrival = Console.ReadLine(); // Read scheduled arrival time
                            break;
                        case RequestType.EmergencyLanding:
                            Console.WriteLine("Enter Estimated Emergency Landing Time (YYYY-MM-DD HH:mm:ss):");
                            scheduledArrival = Console.ReadLine(); // Read estimated emergency landing time
                            Console.WriteLine("Enter Reason for Emergency Landing:");
                            emergencyReason = Console.ReadLine(); // Read reason for emergency landing
                            break;
                    }

                    // Construct Request Data
                    flightRequest = new FlightRequest
                    {
                        RequestType = currentRequestType,
                        FlightNumber = flightNumber,
                        AirplaneId = airplaneId,
                        FuelLevel = fuelLevel,
                        ScheduledArrival = scheduledArrival,
                        ScheduledDeparture = scheduledDeparture,
                        EmergencyReason = emergencyReason
                    };

                    // Serialize the request data to JSON and send it to the server
                    jsonString = JsonSerializer.Serialize(flightRequest);
                    responseData = SendAndReceiveData(clientSocket, jsonString); // Send data and receive response
                    Console.WriteLine("Control tower: " + responseData); // Log server response
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error: {ex.SocketErrorCode}"); // Handle socket exceptions
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}"); // Handle general exceptions
            }
        }

        // Method to send data to the server and receive the response
        static string SendAndReceiveData(Socket client, string dataToSend)
        {
            var requestData = Encoding.UTF8.GetBytes(dataToSend); // Convert the string to byte array
            client.Send(requestData, SocketFlags.None); // Send the data
            var responseData = new byte[1024]; // Buffer for response data
            var bytesRead = client.Receive(responseData); // Receive the response
            var response = Encoding.UTF8.GetString(responseData, 0, bytesRead); // Convert byte array to string
            return response;
        }
    }

    // Class to represent a flight request
    class FlightRequest
    {
        public RequestType RequestType { get; set; } // Request type (Takeoff, Land, EmergencyLanding)
        public int FlightNumber { get; set; } // Flight number
        public int AirplaneId { get; set; } // Airplane ID
        public int FuelLevel { get; set; } // Fuel level
        public string ScheduledArrival { get; set; } // Scheduled arrival time
        public string ScheduledDeparture { get; set; } // Scheduled departure time
        public string EmergencyReason { get; set; } // Reason for emergency landing
    }

    // Enumeration to represent the type of flight request
    enum RequestType
    {
        Takeoff = 1,
        Land,
        EmergencyLanding
    }
}
