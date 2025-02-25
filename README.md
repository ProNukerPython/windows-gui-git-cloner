# Windows GUI Git Cloner

This application allows you to clone a GitHub repository using a graphical user interface (GUI). It supports OAuth authentication for private repositories.

## Prerequisites

- .NET Framework 4.7.2 or later
- Git for Windows
- Administrator privileges

## Installation

1. Download the release package from the [releases page](https://github.com/your-repo/releases).
2. Extract the contents of the ZIP file to a directory of your choice.

## Usage

1. Run the `WindowsGuiGitCloner.exe` as an administrator.
2. Click the `Browse` button to select the target directory where you want to clone the repository.
3. If the repository is private, click the `Sign In` button to authenticate with GitHub.
4. After successful authentication, click the `Clone` button to start cloning the repository.
5. If prompted, choose whether to add lines to `menu.py` automatically.

## Notes

- Ensure that the `LogoGitClonner.jpg` file is in the same directory as the executable.
- The application requires administrator privileges to handle file permissions and delete temporary directories.

## Troubleshooting

- If you encounter any errors, check the status label for detailed error messages.
- Ensure that you have the necessary permissions to access the target directory and delete temporary files.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
