ADSBSharp / BetterSDR
=========
Originally a C# based ADSB decoder written by Youssef Touil and Ian Gilmour.

This branch is a project to port the ModeS Parsing of the excellent dump1090 project to a RTL-SDR Blog compatible windows project.

As i've been refactoring and rebuilding the project, I've also began building an open source C# SDR Project, eventually aiming to compete with the likes of SDR#. 

This portion of the project will likely be moved out into its own repo at some point soon, but for now, this is just a hobby project in my free time! Contributions are welcomed.

Improvements over SDR#
=========
SDR# Utilizes a large amount of "unsafe" code, which is generally a C# Code smell, or something you want to use as infrequently as possible. This project removes 100% of that code, converting it to fully managed code and aims to be written in a more standard manor.

## Things I would like to accomplish
- Compatibility with SDR# Plugins
- More stable and faster than SDR#
- Write a Visual C++ Wrapper of the rtlsdr library so C# P/Invoke wrappers don't need to be used.
