# SYNTHESIZER (SynthGame)

###

<h3 align="left"> 📌 About the project: </h3>
<p align="left"> The project is an application with the ability to connect MIDI devices, developed using C# and GDScript. This project was developed for a graduation thesis. </p>

###

<h3 align="left"> 💾 Cloning the repository: </h3>
<p align="left"> git clone https://github.com/ShDaniCalifornia/SynthGame.git </p>

###

<h3 align="left"> ⚙️ Key features: </h3>
<p align="left"> - Ability to connect MIDI devices (see Connecting a MIDI device); </p>
<p align="left"> - Account registration and login; </p>
<p align="left"> - Completing levels and gaining experience to increase profile level; </p>
<p align="left"> - Changing the sound of a MIDI device by selecting a SoundFont in the game settings (see Installing SoundFont); </p>
<p align="left"> - Switching input and output devices in the game settings. </p>

###

<h3 align="left"> 🛠️ Tech stack and dependencies: </h3>

<p align="left"> 1. Godot 4.3 (GDScript). </p>
<p align="left"> - Acts as the client layer and main managing module, responsible for all user interface (UI), interactive keyboard visualization, as well as game and educational logic (lesson system, authentication, progress). Functions as a host environment, interacting with low-level C# services for audio and MIDI processing. </p>
<p align="left"> 2. Visual Studio 2022 (C#). </p>
<p align="left"> - Serves as the backend, which, using libraries such as NAudio and MeltySynth, provides audio output, MIDI input processing, and user data management via Entity Framework Core. </p>
<p align="left"> - Handles database connectivity and implementation for storing user profile data and lesson progress (SQL Server). </p>
<p align="left"> 3. Libraries used for the Synthesizer project: </p>
<p align="left"> - ManagedBass / ManagedBass.Midi (3.1.1) </p>
<p align="left"> - MeltySynth (2.4.1) </p>
<p align="left"> - NAudio (2.2.1) </p>
<p align="left"> - Microsoft.EntityFrameworkCore (8.0.4) </p>

###

<h3 align="left"> 🎹 Connecting a MIDI device: </h3>
<p align="left"> Several connection methods are available, depending on the type of your MIDI device. </p>

<p align="left"> 1. Direct connection (MIDI-OUT / USB) </p>
<p align="left"> • Connect your MIDI controller to your computer: </p>
<p align="left"> - Via USB cable: If your keyboard has a USB output (usually "USB Type B"), connect it directly. </p>
<p align="left"> - Via MIDI-USB adapter: If your keyboard has only round MIDI-OUT ports, use the appropriate adapter. </p>
<p align="left"> • Typically, the device is detected automatically by the system. </p>

<p align="left"> 2. Bluetooth connection (Using external bridges) </p>
<p align="left"> • Required software: </p>
<p align="left"> - midiLoop (Virtual MIDI Cable): Installed to create a virtual port that SynthGame will be able to see; </p>
<p align="left"> - MidiBerry: Installed to discover your Bluetooth MIDI device and route its signal to the virtual port. </p>

<p align="left"> • Connection procedure: </p>
<p align="left"> - Launch midiLoop and create a virtual port by clicking the plus sign. Make sure an active virtual port (e.g., LoopPort) appears in the list. This port will be used as a bridge; </p>
<p align="left"> - Launch MidiBerry and in the upper part of the interface, find your physical Bluetooth device (e.g., SMK25V2) in the list of available devices; </p>
<p align="left"> - Connect to the device. </p>
<p align="left"> - In the lower part of the MidiBerry interface, route the MIDI output of your Bluetooth device to the virtual port created by midiLoop (e.g., LoopPort). </p>

###

<h3 align="left"> 🔉 Installing SoundFont: </h3>

<p align="left"> For the synthesizer to work correctly, the FluidR3_GM.sf2 SoundFont file is required. It is not included in the Git repository due to its large size. </p>

<p align="left"> 1. Download the file from the following link: https://member.keymusician.com/Member/FluidR3_GM/index.html </p>
<p align="left"> 2. Create a folder named SoundFonts in the project's root directory. </p>
<p align="left"> 3. Copy the downloaded FluidR3_GM.sf2 file into this folder. </p>
