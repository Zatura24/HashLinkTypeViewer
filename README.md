# HashLinkTypeViewer

Simple command line tool to view HashLink objects in runtime.

Currently is hardcoded to be used with Evoland
but this can be changed for other HashLink applications. 

## Usage
- Open `HashLinkTypeViewer.exe`
- Enter an address in the form: 0x00000000
- Press enter

The application will now print a tree like structure HashLink type if found.
For objects it will first print the `super` first and then itself.

## Color coding
- 🟪 Magenta: Object instance (HOBJ)
- ⬜ White: Object fields
- 🟦 Blue: Virtual fields (HVIRTUAL)
- 🟩 Green: Enum types (HENUM)