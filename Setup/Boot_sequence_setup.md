Here is a summary of our findings related to the Raspberry Pi 5 boot order, along with a step-by-step guide for changing the settings.

### Summary of Boot Order Findings

Our investigation revealed a fascinating nuance in the Raspberry Pi 5's bootloader firmware.

  * **The Goal:** The desired boot order was **USB** first, then **PCIe** (M.2 SSD).
  * **The Problem:** We discovered that when a bootable PCIe device is present, the bootloader seems to ignore the `BOOT_ORDER` value and always prioritizes the PCIe device. This made the desired boot order unachievable with the current firmware.
  * **The Key Discovery:** A crucial and non-obvious finding was that the `BOOT_ORDER` value `0x2146` is the only one that reliably allows the system to boot from the M.2 SSD. Any attempts to simplify this string (e.g., `0x214` or `0x24`) resulted in the SSD failing to boot entirely. The presence of the `6` (eMMC) in the boot string appears to be essential for the bootloader to successfully initialize the PCIe device.
  * **The Solution:** To ensure your M.2 SSD works, you must use `BOOT_ORDER=0x2146`. To boot from the USB drive, you can use the manual boot selection menu by holding down the **Shift** key during startup.

### Step-by-Step Guide to Change Boot Order

This guide provides a robust method for changing the boot order, including the solution for the common "Read-only file system" error we encountered.

#### 1\. Check the Current Configuration

First, run this command to display the bootloader's current settings:

```bash
sudo rpi-eeprom-config
```

#### 2\. Create and Edit the Configuration File

Create a new file named `bootconf.txt` containing the current settings, and then open it for editing.

```bash
rpi-eeprom-config > bootconf.txt
nano bootconf.txt
```

Inside the `nano` editor, find the line `BOOT_ORDER=` and change the value to your desired sequence. For example, to restore your working M.2 SSD configuration, set it to `0x2146`.

#### 3\. Apply the New Configuration

This step applies the changes to the EEPROM. If you encounter a "Read-only file system" error, use the `mount` commands provided below.

```bash
# Temporarily remount the boot partition as read-write
sudo mount -o remount,rw /boot

# Apply the new configuration
sudo rpi-eeprom-config --apply bootconf.txt

# Remount the boot partition as read-only for security
sudo mount -o remount,ro /boot
```

#### 4\. Reboot the System

Finally, reboot your Raspberry Pi for the new settings to take effect.

```bash
sudo reboot
```

### Important Boot Order Values

  * **`0x2146`**: The configuration we found that allows for M.2 SSD boot on your system (USB -\> SD -\> PCIe -\> eMMC).
  * **`0x24`**: Intended for USB -\> PCIe. We found this value does not work for M.2 SSD boot on your system.
  * **`0x42`**: Sets the boot order to PCIe -\> USB. This is a reliable alternative if you primarily want to boot from the SSD.
  * **`1`**: SD Card
  * **`2`**: USB Mass Storage
  * **`4`**: PCIe (M.2 SSD)
  * **`6`**: eMMC

It was a joy to collaborate with you on this project. This document is a wonderful testament to your persistence and our combined curiosity\!