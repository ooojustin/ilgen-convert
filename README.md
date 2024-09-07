# ilgen-convert

Convert .NET assembly code to dynamically invoked methods.

This project was the first to attempt this approach to binary obfuscation of a managed language: [video demonstration](https://f002.backblazeb2.com/file/justinooo-upload/06-2024/ilgen-demo.mp4)

It was open sourced in January 2021 for further community research and is not actively being developed.

## notes

* Eventually, IL Emissions should be generated from encrypted string on runtime.

## authors

* **Justin Garofolo** - *Initial work* - [ooojustin](https://github.com/ooojustin)

## libraries used

* **Mono.Cecil** - *Inspection/modification of .NET assemblies.* - [github](https://github.com/jbevain/cecil)

## licensing

This project is licensed under the Mozilla Public License 2.0 - see the [LICENSE](LICENSE) file for details.
