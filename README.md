<p align="center">
  <img src="https://github.com/andrasfuchs/BioBalanceDetector/blob/master/Business/Branding/Logos/BioBalanceDetectorLogo_810x275.png"/>
</p>

# BBD Body Monitor

## Bio-Potential Analysis with Machine Learning for Holistic Health Insights

BBD Body Monitor is a single channel data acquisition system (DAS) to provide a bio-potential reading in the 0.25Hz - 125kHz frequency range. The main focus of the prototype is to use machine learning to detect different mental states (eg. focused wakefulness, relaxation, meditation, REM sleeping, deep sleeping or lucid dreaming) by digitizing potential changes on our bodies. 

If this stage succeeds, fitness and medical data will be integrated into the machine learning framework for early detection of health imbalances. 

It will be a convenient, wearable device having sensors on our forearm or calf giving real-time feedback on our condition.

## Goals
- Detect various mental states such as:
  - Focused wakefulness
  - Relaxation
  - Meditation
  - REM sleep
  - Deep sleep
  - Lucid dreaming
- Recognize different emotions like:
  - Contentment
  - Joyfulness
  - Flow state
  - Anger
  - Frustration
- Correlate fitness and medical data with physical and mental medical conditions for early detection of health imbalances

## Demo

![BBD_2022-10-28_125kHz_75pc-200pc_of_median mp4_20230823_115545 691](https://github.com/Bio-Balance-Detector/bbd-p08-body-monitor/assets/910321/b6a4ee24-2dd2-40be-9f3b-c6fce88d172c)

Watch a video of a rendered recording - https://www.youtube.com/embed/K0LUZHJvA_A

The software gives real-time estimations of various state confidence levels:
![image](https://user-images.githubusercontent.com/910321/171152444-69388f52-aa0c-4665-8ef3-1c761da85a11.png)

## Technical Details

You can read the exact technical requeirements that led to the hardware and software stack choices of the project, and you can set up your own test system using the setup guides. 

If you are brave enough, you can also take a peek at the workflow that I use during my daily work, including the data acquisition, ML model traning and state predictions.

## Setup
It is highly recommended to make measurement with a battery powered setup and preferably in a electromagnetically shielded environment. Radiation from the power grid are usually the strongest, so always try to increase your subject's distance from live cables, outlets and appliances during sampling sessions. External electromagnetic radiation can distort the sampling which can lead to incorrect models in the end.

### Windows
Body Monitor on Windows can provide an excellent performance with 800k SPS, 14 bit resolution and 8192k FFT size. This enables us to have frequency domain data in every 5 seconds in the 0-400 kHz range with 0.1 Hz resolution. 

### Ubuntu

### Raspbian/Raspberry Pi OS

1) Download and install Raspbian OS by following [the official instructions](https://www.raspberrypi.com/software/)
2) Enable SSH and VNC in Preferences / Raspberry Pi Configuration / Interfaces on Raspberry

![image](https://user-images.githubusercontent.com/910321/172062075-f4dbcfc9-8c95-4ed5-90e7-8faa18ef8555.png)

3) Download and install [a VNC client](https://www.realvnc.com/en/connect/download/viewer/windows/) on Windows
4) Connect to "raspberrypi" with the VNC client

![image](https://user-images.githubusercontent.com/910321/172067182-a8a7238c-62f4-4891-9216-e3dd6d139af8.png)

5) [Install .NET 6](https://docs.microsoft.com/en-us/dotnet/core/install/linux-scripted-manual#manual-install) on Raspberry

```
wget https://download.visualstudio.microsoft.com/download/pr/ef1d4f88-cbe9-47c2-9fb3-e7e772be1a4c/4fbbf2a11cee4f52a478a46b1a69ed03/dotnet-sdk-6.0.300-linux-arm.tar.gz

DOTNET_FILE=dotnet-sdk-6.0.300-linux-arm.tar.gz

export DOTNET_ROOT=$(pwd)/.dotnet

mkdir -p "$DOTNET_ROOT" && tar zxf "$DOTNET_FILE" -C "$DOTNET_ROOT"

export PATH=$PATH:$DOTNET_ROOT

nano ~/.bashrc

# add these two lines to the end of the file
# export DOTNET_ROOT=$HOME/.dotnet
# export PATH=$PATH:$HOME/.dotnet

dotnet --version
```

6) Clone this repo

```
git clone https://github.com/andrasfuchs/BioBalanceDetector.git
```

7) Build the source

```
cd ~/BioBalanceDetector/Software/Source/BBDProto08/BBD.BodyMonitor
dotnet publish --configuration release --runtime linux-arm --self-contained true --output ~/BBD.BodyMonitor
```

8) Run the `bbd.bodymonitor` app

```
cd ~/BBD.BodyMonitor
./bbd.bodymonitor
```

9) Download and install [Digilent Adept Runtime](https://lp.digilent.com/complete-adept-runtime-download)
  * For Raspberry OS 32-bit: Raspberry Pi
    * To remove: `sudo dpkg --remove digilent.adept.runtime:armhf`
  * For Raspberry OS 64-bit: ARM / Download 64-bit .deb

11) Download and install [Digilent WaveForms](https://forum.digilent.com/topic/8908-waveforms-beta-download/)
  * For Raspberry OS 32-bit: digilent.waveforms*armhf.deb
    * To remove: `sudo dpkg --remove digilent.waveforms:armhf`
  * For Raspberry OS 64-bit: digilent.waveforms*arm64.deb

### Android
Using the Body Monitor on Android makes it mobile, but it still needs another computer on the same network to process and save the data. Sampling rate is limited to 250k SPS, it drains the phone battery in 30-60 minutes depending on the phone, and it needs a fast and stable WiFi in order to work.

1) Buy [a $10 OTG cable](https://www.amazon.de/-/en/UGREEN-Adapter-Laptop-Compatible-MacBook/dp/B072V9CNTK) and connect AD2 to your phone
2) Buy [the VirtualHere USB Server](https://play.google.com/store/apps/details?id=com.virtualhere.androidserver) for $25 and install it on your phone
3) Download [the VirtualHere USB Client](https://www.virtualhere.com/usb_client_software) to your Windows or Linux and run it
4) Setup your Windows or Linux as described above
5) Before you start the data acquisition make sure that you select the "Digilent USB device" in the VirtualHere client and you select "Use this device"
6) Run the `bbd.bodymonitor` as you would normally do

![Hardware Components](./Photos/Android_mobile_configuration.jpg) 

## Workflow
0) Calibration (optional)

Calibration is recommended to be done only once when the device is connected to a new host to know the amount of potential measurement error. To start the calibration process you must connect the W2 output directly to the CH1 input with a BNC cable and start the Body Monitor app with the following command:

`bbd.bodymonitor.exe --calibrate`

You will get an estimated error range for different amplitudes and frequencies.
![image](https://user-images.githubusercontent.com/910321/171175369-ab81877f-6e17-429e-9ccc-0a1b62e589d5.png)

1) Data acquisition

To start a data acquisition you must start the Body Monitor app with the following command:

`bbd.bodymonitor.exe`

This will save the detailed, timecoded FFT data as a binary file and the recorded audio as an MP3 file every 5 seconds. With the highest setting of 800kSps and 8192k FFT it needs a drive with at least ~3.5 MB/s of sequential writing speed.

The data in first saved in the root data directory (as defined in the `appsettings.json`) in a sub-directory representing the current date in the `yyyy-MM-dd` format. 

2) Labeling/tagging

As a next step we need to move those files into further sub-directories that represent the labels that we want to use for training later.
For example you can see that there are labels like `#Subject_AndrasFuchs`, `#Subject_None`, `#BloodTest_Beforehand` and `#BloodTest_Thereafter` in this screenshot:

![image](https://user-images.githubusercontent.com/910321/171133110-22a1b3f9-3c8a-432e-a79d-bc207bef56e5.png)

Labels start with the hash `#` character, and they are usually grouped if they are categorical, and we use the underscore `_` character as the group separator. This means basically that data with the `#Subject` label can have either the value of `AndrasFuchs` or `None`, but not both at the same time.

The body monitor also records audio as 5-second-long MP3s if a microphone is connected to it, and configured properly. It can help a lot with labeling.

3) Generating CSV files for machine learning

To start a data acquisition you must complete your labeling and copy all your training data into a folder and then start the Body Monitor app with the following command:

`bbd.bodymonitor.exe --mlcsv TrainingData "MLP05" "Subject_" "Subject_None"`

![image](https://user-images.githubusercontent.com/910321/171265500-f9f0d72d-6f6c-44c5-bccc-79114802feea.png)

As you can see my activities and conditions like training, meditation, etc. are represented as sub-directories that are handled as labels.

Machine learning frameworks usually need a data structure similar to database tables, where every row represents a sampling and these rows contain input values (called `features`) and values that will be predicted by the AI model (called `labels`). In our case features are the sampled frequency values and we have only one selected label to predict per model.

Since normally we would have 4 million input values, because our bandwidth is 400kHz and our resolution is 0.1 Hz we need to transform that amount to data into a smaller size. Frameworks like ML.NET are not prepared to handle 4 million input parameters at the moment. Such a conversion is done by defining machine learning profiles in the `appsettings.json`. 

![image](https://user-images.githubusercontent.com/910321/171385837-a7270beb-ca7b-4591-baf2-ffb87a6c5138.png)

For example we select the `MLP05` profile which converts the original high fidelity data into a 0.5 Hz resolution data series in the 0.5 Hz - 200 Hz frequency range.

![image](https://user-images.githubusercontent.com/910321/171373830-4f4047d7-376b-48fb-81a3-0efc6641e1a9.png)

This now-converted data will be compiled in the CSV file is the data entry has at least one label starting with `Subject_` and the value of `Subject_None` will be appended to that particular line. If the label is categorical then its value will be 1.00 if it's set and 0.00 if it's not.

![image](https://user-images.githubusercontent.com/910321/171141828-e98c5fc6-a380-4453-a46b-0756fd32d0cb.png)

The command above will also generate the `.mbconfig` file that configures ML.NET Model Builder for the model training.

4) Building AI models

The CSV files are fed to the [ML.NET Model Builder 2022](https://dotnet.microsoft.com/en-us/apps/machinelearning-ai/ml-dotnet/model-builder) to produce the trained models. Thanks to the prepared `.mbconfig` file, it needs no configuration, so the training can begin right after opening the file in Visual Studio.

![image](https://user-images.githubusercontent.com/910321/171156775-461a2252-6161-44e3-90a2-3e9b00a86682.png)

5) Model integration

Those models get integrated into the application to provide real-time feedback.

In the following session the sensor band is not used in the first ~30 seconds, then I take it on my left forearm, and they I take it off again. Look how the `Not attached?` and `Andras?` column values change over time.

![image](https://user-images.githubusercontent.com/910321/171152444-69388f52-aa0c-4665-8ef3-1c761da85a11.png)

The values generated by the models are not perfect, of course, but they give a good indication even with such a small sample count and reduced bandwidth and resolution.

6) Model testing

There is a way to test all the current models. We can generate their confusion matrix, that show how many times guesses the model the outcome correctly. You can test with a certain percentage of the data in a given folder with the following command line:

`bbd.bodymonitor.exe --testmodels TrainingData 5%`

The confusion matrix of a model gives us an estimate of the true positive, true negative, false positive, false negative case count and its overall performance.

![image](https://user-images.githubusercontent.com/910321/171387607-bff07e7e-554a-479b-8c61-4f060a71b9eb.png)

7) Video generation (optional)

Generating video about a session could help understand in an intuitive way what happened during that session. The following sample videos were generated on a PC from the raw data that was generated on either on Raspberry Pi or a Windows workstation.

* 2022-10-30, 0.25 Hz - 125 kHz, 100 uV,  5x speed: https://youtu.be/K0LUZHJvA_A
* 2022-05-31, 0.0 Hz to 800  Hz, 500 uV, 60x speed: https://youtu.be/TAWqZWBXpZg
* 2022-05-31, 0.0 Hz to 150 kHz, 100 uV, 60x speed: https://youtu.be/ZTBsz2m_lBs
* 2022-05-31, 0.0 Hz to 398 kHz, 750 uV, 60x speed: https://youtu.be/88tr0x3rEP8
* 2021-01-21, 0.2 Hz to 100 kHz, 60x speed: https://www.youtube.com/watch?v=Kx8WTaREeTs
* 2021-01-21, 0.2 Hz to   2 kHz, 60x speed: https://www.youtube.com/watch?v=u7TJLMuWv-U

![BBD_2022-10-28_125kHz_75pc-200pc_of_median mp4_20230823_115545 691](https://github.com/Bio-Balance-Detector/bbd-p08-body-monitor/assets/910321/b6a4ee24-2dd2-40be-9f3b-c6fce88d172c)
  
## Conclusion

The project is actively ongoing and the results are promising. Detecting different people and their different states seems to be possible in a reliable way. 

The question still remains: is it possible to detect health issues with this technique?

As a next step I will need to collect as much data as possible, so I'm looking for healthcare providers to work with. One such potential cooperation could be done with labs that analyze blood samples. If I could make quick measurements before/during/after the blood sampling and I had enough (anonymized) data to look for correlations between my measurements and the blood test results, it would potentially lead to a new, non-invasive, cheap way to check for health imbalances.
