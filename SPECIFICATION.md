# DataOnQ - Specification
The DataOnQ Specification is built off of a real-world internal library that [Andrew Hoefling (@ahoefling)](https://github.com/ahoefling) built for FileOnQ on a Xamarin.Forms project. If you are interested in why this library was built or some of the background leading up to the project, check out the [Problem Space](PROBLEM_SPACE.md) and our [Vision](VISION.md).

# Roadmap
This specification is a living document and is subject to change. Below is a table of the current status of the Specification.

| Milestone                                                          | Status             |
|--------------------------------------------------------------------|--------------------|
| [Core Specification](SPECIFICATION_CORE.md)                        | ✔ Done             |
| [Pre-Built Middleware Specification](SPECIFICATION_MIDDLEWARE.md)  | ✔ Done     |
| [Plugin Specification](SPECIFICATION_PLUGIN.md)                                           | 🗺 Planned         |

## Core Library (Advanced)
The core library of DataOnQ is described in the [Core Specification]() file. This goes into detail of the inner workings of DataOnQ and how a development team can extend the platform for their own custom offline synchronization workflows.

* [Core Specification](SPECIFICATION_CORE.md)

The core library includes everything in DataOnQ:
* Building a Custom Middleware
  * Including the Binary Tree Management
* Service Handler Implementations
* DataOnQ Startup Logic

## Pre-Built Middleware (Intermediate)
The DataOnQ Handler libraries is an easy way to get started with DataOnQ and is described in the [DataOnQ Handlers]() file. A handler implementation utilizes a pre-built DataOnQ Middleware, where the development team will need to implement several different handlers.

* [DataOnQ Middleware](SPECIFICATION_MIDDLEWARE.md)

A DataOnQ Handler Implementation provides a balance mix of ease of use and customization for your offline workflows. 
* Service Handler Implementations
* DataOnQ Startup Logic

## DataOnQ Plugins (Beginners)
If you want to get started as quickly as possible the DataOnQ Plugins allow you to hook into your code with 0 additional code. The DataOnQ Plugins are highly opinionated require your application to follow the patterns in place.

Current the plugin framework is still be considered, if you have feedback based on the Core library and Handlers please leave feedback in a GitHub issue.

* TBD
