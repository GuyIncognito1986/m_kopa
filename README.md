# M-Kopa Technical Test

### Problems we are trying to solve:

- Well tested and validated at least once delivery of SMS message
- Not real implementation with connectivity just a contract (interface) based abstraction which can be modelled as a state machine of command received -> HttpRequest sent + Successful HttpResponse received -> Request processed to event hub. (Other states in there for retries for instance if some response code fails etc)
- No concrete implementations of any clients or the actual service executable as stated in the exercise 

### Patterns used:

- This is just a state machine, no point creating extra complexity etc, I'm not the biggest fan of going full on CQRS with event sourcing.

### Assumptions/Choices/Justifications:

- All internal serialization/deserialization (taking message from message queue, putting it to service bus etc) is in byte array format (thinking MessagePack here in real life) and the interfaces will reflect such, not going to tie anything to concrete implementations etc, but taking byte[] over the interface for pub/sub clients simplifies things 
- Taking away from our chat with Juan and Henry the message queue for the input is an event hub, therefore subscription to topic/event hub means IAsyncEnumerable coming from the client, the exercise says components not chosen but for the contract I'm going to use IAsyncEnumerable being returned from subscribe method.
- Containerization/k8s etc out of scope, running as a process (not windows service etc) in scope. (In real life this would probably run in k8s etc and on a linux container therefore 1 process per container etc.
- Since this is a microservice it needs to run with horizontal scaling for processing sms sending so the state is held else where, for instance in a state management service or redis cache running in persistent AOF mode.
- Adding some sort of a correlation id to the instruction. Going to use a CUID2 because it's the future or something (more performant lighter, etc), in real life this would probably mean everywhere else would have to support cuid2s, so in a large microservice environment we'd probably go with GUIDs until everyone could make that jump, but this is just an exercise so going with "ideal" solution. This is so we can easily correlate the state with the sms event when replaying the queue on service startup. There is also nanoId, but it seems less popular and less mature etc.
- Writing in .net 7, I think this is better then LTS releases as it forces small incremental improvements/upgrades to .net versions during development/support of development lifecycle, vs jumping LTS versions where more tech debt would need to be addressed in a "big bang" fashion.
- Acceptance tests in fluent assertions over specflow. I get the need for BDD style tests etc, but specflow is just a horrible implementation using code gen etc, quite often buggy and massively lagging behind actual .net versions etc. Also historically used to make visual studio crawl.
- No unicode chars in the message, so utf-8 encoding, so limit is 160 characters, I'm sure there is some Twilio library out there which can do all this sms validation for us etc, but I'm keeping it light, saying that I am using libphonenumber-csharp for actual number validation.
- If a message is a multi-sms message, they will come through as different messages from the hub with different correlation ids. Aka we do not handle message splitting inside the sms worker. (This is a little lazy on our part as sms delivery is mission critical according to the spec, HOWEVER it does force separation of responsibilities on the service layer etc and delegates it an sms compositor service where it should be.)
- If any of the above validations are broken, we push the message to a dead letter queue and log an error. 
- There is schema validation on the message queue so that we don't need to handle/have a dead letter queue on incoming messages.
- Assuming localization for errors/messages in the logs etc is fine as strings in code so I don't have to muck about with resx files etc, as it's for internal non customer facing logging.
