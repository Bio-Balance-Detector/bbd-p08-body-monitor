# AI Agent Context for BBD Body Monitor

This document provides essential context for an AI code agent to understand and make modifications to the BBD Body Monitor project.

## Project Name

BBD Body Monitor (Bio Balance Detector)

## Overall Project Description

The BBD Body Monitor is a system designed for acquiring, analyzing, and interpreting bio-potential data from the human body. The primary goal is to use machine learning to identify different mental and physiological states (e.g., wakefulness, relaxation, sleep stages, stress). This is achieved by digitizing potential changes on the body, primarily from the forearm or calf, using a single-channel data acquisition system.

The system processes these signals in the 0.25Hz - 125kHz frequency range. It leverages Fast Fourier Transform (FFT) to analyze frequency components. The long-term vision is to correlate these bio-potential readings with other health data (e.g., from Fitbit, medical history, blood tests) for early detection of health imbalances. The project also explores potential treatment applications through a signal generator module.

The BBD Body Monitor is designed to be a convenient and wearable device, often utilizing a Raspberry Pi for portability and processing. It supports various platforms including Windows, Linux, and Android (for data acquisition).

## Tech Stack

### Hardware Components
*   **Analog Discovery 2:** National Instruments device used as a 100MS/s USB oscilloscope, logic analyzer, and variable power supply. This is the core data acquisition (ADC) hardware.
*   **Raspberry Pi 4 Model B 4GB:** Often used as the central processing unit in portable setups.
*   **Silver-impregnated wristband:** Acts as the sensor to pick up bio-potentials from the body.
*   **20000 mAh Power Bank:** To power the Raspberry Pi and Analog Discovery 2 for extended mobile operation (targeting 8+ hours).
*   **USB Microphone:** Used for recording audio snippets concurrently with bio-potential data. This audio is used for tagging and labeling data segments, which aids in the machine learning workflow.

### Software Platforms
*   **Windows (10/11):** Used for development, AI model training (ML.NET Model Builder currently Windows-only), and as a workstation for running the system.
*   **Linux (Ubuntu, Raspbian/Raspberry Pi OS):** Used for deployment on Raspberry Pi and as a workstation.
*   **Android:** Supported for data acquisition in a mobile scenario. This typically involves using the VirtualHere USB Server app to share the Analog Discovery 2 over IP to a machine running the main BBD software.

### Core Software Technologies
*   **.NET 9:** The primary development framework for the entire software suite. (Specified in `Source/global.json`)
*   **C#:** The programming language used for all backend logic, APIs, core libraries, and the web frontend.
*   **ASP.NET Core:** Framework used to build the backend REST API (`BBD.BodyMonitor.API`).
*   **Blazor WebAssembly:** Used for the client-side frontend web application (`BBD.BodyMonitor.Web`), providing an interactive user interface.
*   **ML.NET 2.0:** The machine learning framework used for training models to recognize patterns in bio-potential data and predict states.
*   **JSON (JavaScript Object Notation):**
    *   Used extensively for configuration files (e.g., `appsettings.json` in API and Web projects, `Data/Metadata/GlobalSettings.json`).
    *   Used for storing metadata related to sessions, subjects, and locations (`Data/Metadata/`).
*   **EDF (European Data Format):** A standard file format for biomedical time series data. Presence of `Source/BBD.BodyMonitor/EDF/EDFFile.cs` suggests capability to read or write EDF files.
*   **MP3:** Audio recordings for data labeling are saved in this format.
*   **CSV (Comma-Separated Values):** Data is transformed into CSV files for input into the ML.NET Model Builder.

### Key Libraries and Integrations
*   **Fitbit.Portable (`Source/Fitbit.Portable/`):** A custom C# library for interacting with the Fitbit API. It handles OAuth2 authentication and allows fetching various data types like sleep, activities, heart rate, etc.
*   **Digilent WaveForms & Adept Runtime:** Software required to interface with the Analog Discovery 2 hardware. The `dwf.cs` file (`Source/BBD.BodyMonitor.API/Interop/dwf.cs`) is a C# wrapper for the native Digilent WaveForms SDK.
*   **VirtualHere:** Third-party software used to enable USB-over-IP for connecting the Analog Discovery 2 to an Android phone and then to a processing computer.
*   **Serilog:** (Implied by `Logging` in API models) A popular logging library for .NET applications.

## Repository Structure and File/Folder Descriptions

*   **`.gitignore`, `.gitmodules`:** Standard Git files for managing the repository.
*   **`Data/`:** Stores application data.
    *   `Metadata/`: Contains JSON files organizing the collected data.
        *   `GlobalSettings.json`: Global configuration for the application.
        *   `Locations/`: JSON files describing different measurement locations.
        *   `Sessions/`: JSON files detailing individual data collection sessions (e.g., `BBD_20221020_143285__0xAF34D2.json`).
        *   `Subjects/`: JSON files with information about test subjects.
    *   `TrainingData/`: Intended to store data prepared for machine learning (e.g., CSV files). `placeholder.txt` is a placeholder.
*   **`Documentation/`:** Contains markdown files providing project documentation.
    *   `Setup.md`: Detailed setup instructions for Windows, Raspberry Pi, and Android.
    *   `TechnicalRequirements.md`: Project goals, hardware/software stack.
    *   `Workflow.md`: Step-by-step guide of the data collection and ML process.
    *   `AI_AGENT_CONTEXT.md`: This file.
*   **`LICENSE`:** Project's license file.
*   **`Notebooks/`:**
    *   `BBDModel.zip`: Likely contains a pre-trained ML.NET model or related assets.
    *   `LargeDataset.ipynb`: A Jupyter Notebook, probably used for data exploration, custom analysis, or experimenting with ML models outside the standard ML.NET workflow.
*   **`Photos/`:** Images of hardware components and setup diagrams.
*   **`README.md`:** The main entry point for understanding the project, its goals, impact, and how to get started.
*   **`Setup/`:**
    *   `Raspberry_Setup.txt`: Additional setup notes specifically for Raspberry Pi.
*   **`Source/`:** Contains all the source code for the project.
    *   `BBD.BodyMonitor/`: A core C# class library containing the fundamental logic.
        *   `Buffering/`: Classes for managing data buffers (e.g., `ShiftingBuffer.cs`).
        *   `Configuration/`: Defines various options classes (e.g., `BodyMonitorOptions.cs`, `AcqusitionOptions.cs`, `MachineLearningOptions.cs`) loaded from `appsettings.json`.
        *   `EDF/`: Contains `EDFFile.cs` for handling EDF formatted files.
        *   `Environment/`: Classes related to system information and connected devices.
        *   `FftBin.cs`, `FftData.cs`, `FftDataV1.cs`, `FftDataV2.cs`, `FftDataV3.cs`: Classes representing FFT data structures.
        *   `Filters/`: Code for data filtering (e.g., `FftDataFilters.cs`).
        *   `Indicators/`: Logic for evaluating physiological indicators.
        *   `MLProfiles/`: Specific C# classes defining different machine learning profiles (e.g., `MLP05.cs`, `MLP09.cs`). These control how raw FFT data is transformed for ML.
        *   `Sessions/`: Manages session-related data including subjects, locations, and identity.
    *   `BBD.BodyMonitor.API/`: The ASP.NET Core backend API project.
        *   `Controllers/`: API endpoints (e.g., `DataAcquisitionController.cs`, `FitbitController.cs`, `MachineLearningController.cs`).
        *   `Interop/dwf.cs`: C# wrapper for the Digilent WaveForms library.
        *   `Models/`: Data Transfer Objects (DTOs) and models used by the API (e.g., `FftModelInput.cs`, `FftModelOutput.cs`, `MBConfig.cs` for ML.NET Model Builder configuration).
        *   `Services/`: Service classes containing the business logic (e.g., `DataProcessorService.cs`, `SessionManagerService.cs`).
        *   `appsettings.json`, `appsettings.Development.json`: Configuration files for the API.
    *   `BBD.BodyMonitor.Web/`: The Blazor WebAssembly frontend application.
        *   `Pages/`: Blazor components for different UI views/pages (e.g., `Acquisition.razor`, `Dashboard.razor`).
        *   `Shared/`: Shared Blazor components (e.g., `MainLayout.razor`, `NavMenu.razor`).
        *   `Data/BioBalanceDetectorService.cs`: A service that handles communication between the Blazor app and the backend API.
        *   `wwwroot/`: Static web assets (CSS, JavaScript, images, fonts).
        *   `appsettings.json`, `appsettings.Development.json`: Configuration files for the Blazor app.
    *   `BBD.BodyMonitor.sln`: The Visual Studio solution file, which groups all related projects.
    *   `Fitbit.Portable/`: A separate C# portable class library for interacting with the Fitbit API. Handles OAuth, data serialization, and API calls for various Fitbit data types (activity, sleep, heart rate, etc.).
    *   `global.json`: Specifies the .NET SDK version (6.0.300) to be used for building the projects.
    *   `EDF` (directory): This appears to be a separate, possibly older or alternative project/library related to EDF. Its exact current usage in conjunction with `Source/BBD.BodyMonitor/EDF/` might require deeper analysis if EDF processing is a key task.

## Data Acquisition and Processing Workflow

1.  **Calibration (Optional):**
    *   Performed once per new host to estimate measurement error.
    *   Done by connecting the Analog Discovery 2's W2 output directly to CH1 input.
    *   Initiated by running `bbd.bodymonitor.exe --calibrate`.
2.  **Data Acquisition:**
    *   The Analog Discovery 2, via its BNC interface and connected wristband sensor, captures bio-potential signals.
    *   The `bbd.bodymonitor.exe` application (or its equivalent on Linux/Raspberry Pi) manages this process.
    *   Data is sampled at high rates (e.g., up to 800k SPS).
    *   FFT (Fast Fourier Transform) is calculated on the fly, typically for 5-second blocks of data.
    *   Raw FFT data is saved as binary files.
    *   Simultaneously, if a microphone is configured, 5-second audio snippets are recorded and saved as MP3 files. These are used for annotating the data.
    *   Data is initially saved in a root data directory (defined in `appsettings.json`) under a sub-directory named with the current date (`yyyy-MM-dd`).
3.  **Labeling/Tagging:**
    *   This is a crucial manual or semi-automated step.
    *   Users move the collected data files (FFT binaries and MP3s) into further sub-directories.
    *   Directory names act as labels (e.g., `#Subject_AndrasFuchs`, `#Activity_Meditating`, `#Condition_Relaxed`).
    *   Labels starting with `#` are used. Underscores often separate label categories from values (e.g., `Subject_` is the category, `AndrasFuchs` is the value).
    *   Audio recordings help in accurately labeling the corresponding bio-potential data segments by providing context of what was happening at the time of recording.
4.  **Generating CSV Files for Machine Learning:**
    *   After labeling, data is prepared for ML.NET.
    *   Command: `bbd.bodymonitor.exe --mlcsv TrainingData "MLP05" "Subject_" "Subject_None"`
        *   `TrainingData`: Path to the folder containing labeled data.
        *   `"MLP05"`: Specifies a Machine Learning Profile (e.g., `MLP05` from `appsettings.json` or `MLProfiles/MLP05.cs`) which defines how the high-resolution FFT data is downsampled or transformed (e.g., to 0.5 Hz resolution in the 0.5 Hz - 200 Hz range). This is necessary because ML frameworks might not handle millions of input features efficiently.
        *   `"Subject_"`: The primary label category to predict.
        *   `"Subject_None"`: A specific value of the label to be appended.
    *   This process generates:
        *   A CSV file where each row is a sample, features are derived from FFT data (transformed by the ML Profile), and columns represent labels.
        *   An `.mbconfig` file, which is a configuration file for ML.NET Model Builder, pre-setting it for training.

## Machine Learning Aspect

*   **ML.NET Role:** Used as the primary framework for building and running machine learning models.
*   **ML Profiles:**
    *   Defined in `appsettings.json` and implemented in C# classes within `Source/BBD.BodyMonitor/MLProfiles/`.
    *   Essential for feature engineering: they reduce the dimensionality of the raw FFT data (which can have millions of data points) to a manageable number of features for ML.NET. This involves selecting frequency ranges, resolutions, and potentially other transformations.
*   **Model Training:**
    *   The generated CSV and `.mbconfig` files are used with ML.NET Model Builder (typically within Visual Studio on Windows).
    *   Model Builder automates the process of trying different algorithms and hyperparameters to find a suitable model for the classification task (predicting labels like subject identity or mental state).
*   **Model Integration & Real-Time Feedback:**
    *   Trained models (usually `.zip` files produced by ML.NET) are integrated back into the `BBD.BodyMonitor` application.
    *   The application can then load these models and provide real-time (or near real-time) predictions based on incoming bio-potential data. The console output and web dashboard can display these predictions (e.g., confidence levels for states like "Not attached?", "Andras?").
*   **Model Testing:**
    *   The application supports testing trained models.
    *   Command: `bbd.bodymonitor.exe --testmodels TrainingData 5%`
    *   This command uses a portion of the labeled data (e.g., 5%) to evaluate the models and generates confusion matrices, which show performance metrics like true positives, true negatives, false positives, and false negatives.

## Testing Framework

The BBD Body Monitor project incorporates several testing strategies to ensure code quality and model accuracy.

*   **Unit Testing:** The solution includes dedicated test projects for various components, likely using a framework like MSTest or xUnit, given the .NET environment:
    *   `Source/BBD.BodyMonitor.API.Tests/`: Contains tests for the ASP.NET Core backend API, ensuring controllers and services function as expected.
    *   `Source/BBD.BodyMonitor.Tests/`: Houses tests for the core `BBD.BodyMonitor` library, validating its business logic, data processing, and utility functions.
    *   `Source/BBD.BodyMonitor.Web.Tests/`: Includes tests for the Blazor WebAssembly frontend application, potentially covering component behavior and service interactions.
    *   `Source/Fitbit.Portable.Tests/`: Contains tests for the `Fitbit.Portable` library, verifying its interaction with the Fitbit API and data models.
*   **Machine Learning Model Testing:**
    *   The application provides a specific command-line interface for evaluating the performance of trained machine learning models.
    *   Command: `bbd.bodymonitor.exe --testmodels TrainingData 5%`
    *   This command typically uses a specified percentage (e.g., 5%) of the labeled data from the `TrainingData` directory to test the models. It often generates confusion matrices and other metrics to assess the accuracy of predictions for different states or subjects.

This approach ensures that individual components are functioning correctly and that the machine learning models perform adequately on unseen data.

## Key Features and Functionalities

*   **Real-time Bio-potential Analysis:** Acquisition and FFT processing of bio-signals.
*   **Mental State Detection:** Core ML goal to classify states like focus, relaxation, sleep stages.
*   **Subject Identification:** ML models can also be trained to identify the subject wearing the device.
*   **Fitbit Integration:** Fetches data from Fitbit devices for a more holistic health picture.
*   **Data Logging and Storage:** Systematic storage of raw and processed data, including metadata.
*   **Audio-Assisted Labeling:** Microphone input for annotating data segments.
*   **Calibration Process:** To account for hardware measurement errors.
*   **Configurable ML Profiles:** Flexible feature engineering for machine learning.
*   **Web-Based Dashboard:** The Blazor application provides a UI for viewing data and system status.
*   **Signal Generation:** The Analog Discovery 2 has signal generation capabilities, which are mentioned as having "untapped potential for treatment applications," though this feature seems less developed currently.

## Setup and Deployment

*   **Supported Platforms:** Windows, Linux (including Raspberry Pi OS), and Android (for data capture via USB/IP).
*   **Detailed Instructions:** Available in `Documentation/Setup.md` and `Setup/Raspberry_Setup.txt`.
*   **Core Application:** `bbd.bodymonitor.exe` (or its Linux/ARM equivalent compiled from `Source/BBD.BodyMonitor.API/` or `Source/BBD.BodyMonitor/` - the documentation mentions `Source/BBDProto08/BBD.BodyMonitor` which might be an older path or a specific build target). The API project (`BBD.BodyMonitor.API`) seems to be the main executable that hosts services and potentially the Blazor app.

## Other Relevant Information

*   **Portability:** A key design goal, facilitated by Raspberry Pi and power bank.
*   **Battery Power:** Emphasized to avoid 50/60 Hz mains interference in sensitive bio-potential measurements.
*   **Iterative Prototyping:** The project is described as the "8th prototype," indicating a history of iterative development.
*   **Open to Collaboration:** The project owner is actively seeking collaboration with healthcare practitioners to collect more data and validate the system's efficacy in detecting health issues.
*   **Video Generation:** An optional workflow step allows generating videos that visualize session data, which can be helpful for intuitive understanding.

This document should serve as a good starting point for an AI agent tasked with working on the BBD Body Monitor codebase.
