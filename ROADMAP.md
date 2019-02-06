# Roadmap for neo-debugger-tools

# Rationale
Make NEO Blockchain development as easy as ‘regular’ software development, using a tool that is able to build and test app development (smart contracts), and also server (node) development, monitoring, and integration.
It should support all major OS (Windows, Linux, and MacOS) and the main programming languages supported to build NEO smart-contracts.

# Additional Infos
Several features are already implemented in a WindowsForm version. It works well in day-to-day development, but unfortunately, it only works on windows machines.
It runs your code in an emulator, which is good because it allows real-time debugging, but it may not reflect real blockchain behavior.

## Version 0.1.0 - Short Term - Basic Cross Platform Support
* Cross-platform support using AvaloniaUI and AvaloniaEdit;

## Version 0.2.x - Medium Term - Initial Blockchain Integration
* Configure network;
* Deploy SmartContract;
* RPC integration;
* Multi-signature support;
* Get invocation results;
* Retrieve invocation notifications;
* Export TestCases to JSON RPC tests.

## Version 0.3.x - Medium Term - UI Revamp
* New interface feature list;
* New interface wireframes;
* New interface identity;
* Cross platform using JS: VS Code extension, electron standalone app or webpage. (to be discussed);
* Present features migration into new UI;

## Version 0.4.x - Medium Term - Add Go and Java support
* Add Java debugging;
* Add Java examples;
* Add Go debugging;
* Add Go examples;

## Version 1.0.0 - Long Term - Advanced blockchain integration and manipulation
* Distributed blockchain deployment and monitoring using AWS;
* Run tests / scripts from remote nodes;
* Node monitoring and profiling;
* Tests results interface;

### Original Authors
@relfos
@gubanotorious

### Contributors
@lock9
@melanke
