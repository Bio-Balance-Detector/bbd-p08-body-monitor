<p align="center">
  <img src="https://github.com/andrasfuchs/BioBalanceDetector/blob/master/Business/Branding/Logos/BioBalanceDetectorLogo_810x275.png"/>
</p>

# BBD Body Monitor

## Bio-Potential Analysis with Machine Learning for Holistic Health Insights

BBD Body Monitor is a single channel data acquisition system (DAS) to provide a bio-potential reading in the 0.25Hz - 125kHz frequency range for individuals who wish to better understand their own bio-potentials. The main focus of the prototype is to use machine learning to detect different mental states (eg. focused wakefulness, relaxation, meditation, REM sleeping, deep sleeping or lucid dreaming) by digitizing potential changes on our bodies. 

If this stage succeeds, fitness and medical data will be integrated into the machine learning framework for early detection of health imbalances. 

We're designing it to be a convenient wearable device, with sensors located on the forearm or calf, providing real-time feedback on your condition. 

## Goals
- Detect various mental states such as:
  - Focused wakefulness
  - Relaxation
  - Meditation
  - REM sleep
  - Deep sleep
  - Lucid dreaming
- Detect various emotions such as:
  - Contentment
  - Joyfulness
  - Flow state
  - Anger
  - Frustration
- Correlate fitness and medical data with physical and mental medical conditions for early detection of health imbalances

## Demo

![BBD_2022-10-28_125kHz_75pc-200pc_of_median mp4_20230823_115545 691](https://github.com/Bio-Balance-Detector/bbd-p08-body-monitor/assets/910321/b6a4ee24-2dd2-40be-9f3b-c6fce88d172c)

Watch the BBD Body Monitor in Action! - https://www.youtube.com/embed/K0LUZHJvA_A

The software gives real-time estimations of various state confidence levels:
![image](https://user-images.githubusercontent.com/910321/171152444-69388f52-aa0c-4665-8ef3-1c761da85a11.png)

## Technical Details

If you're curious about the nuts and bolts behind BBD Body Monitor, delve into our [technical requirements](/Documentation/TechnicalRequirements.md). Want to set up your own test system? Check out our [setup guides](/Documentation/Setup.md) tailored for Windows, Linux, and Raspberry Pi platforms.

If you are brave enough, you can even have a peek at [the workflow](/Documentation/Workflow.md) that I use during my daily work, including the data acquisition, ML model traning and state predictions.
 
## Conclusion

The project is actively ongoing and the results are promising. Detecting different people and their different states seems to be possible in a reliable way. 

**The core question remains**: Can this technique reliably detect health issues?

As a next step I will need to collect as much data as possible, so I'm looking for healthcare providers to work with. One such potential cooperation could be done with labs that analyze blood samples. If I could make quick measurements before/during/after the blood sampling and I had enough (anonymized) data to look for correlations between my measurements and the blood test results, it would potentially lead to a new, non-invasive, cheap way to check for health imbalances.

If you're a healthcare provider interested in collaborating, please [reach out](mailto:info@biobalancedetector.com).

## Contributing

I truly appreciate your interest in BBD Body Monitor. While code contributions are valuable, reporting issues is equally critical. It's feedback from users like you that allows us to continuously improve BBD Body Monitor. Here's how you can help:

### Submitting Issues

* Make sure you have a [GitHub account](https://github.com/signup/free).
* Search the issue tracker to ensure your report is a new issue. Duplicates will be closed.
* Create a new issue, being as descriptive as possible:
  * Clearly describe the issue, including steps to reproduce if it's a bug.
  * Include screenshots or videos if they help to clarify the problem.
  * Mention which version of BBD Body Monitor you're using, and on what platform.
  * Use a clear and descriptive title for the issue to identify the problem.

#### Benefits of Submitting Issues

1. **Voice Your Needs & Problems:** By sharing what you need or what's not working, you help guide the development direction.
2. **Join the Community Conversation:** Engage in discussions, and help us understand the context and importance of the problems you face.
3. **Faster Fixes:** Clear, well-documented issues often receive quicker solutions.

### Contributing Code

While I emphasize issue submissions, if you're a developer willing to fix an issue you've experienced, I'm not going to stop you! However, please start with submitting the issue first. Here's a brief guide if you decide to contribute with code:

* Fork the repository on GitHub.
* Create a topic branch from the `main` branch.
* Ensure you've added or updated any necessary tests for your changes.
* Make sure all tests pass before submitting a pull request.

### Additional Resources

* [General GitHub documentation](https://help.github.com/)
* [GitHub issue submission tutorial](https://help.github.com/articles/creating-an-issue/)
* [GitHub pull request documentation](https://help.github.com/send-pull-requests/)
