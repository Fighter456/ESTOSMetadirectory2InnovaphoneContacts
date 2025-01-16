# ESTOSMetadirectory2InnovaphoneContacts

ESTOSMetadirectory2InnovaphoneContacts is a Windows service that helps to convert and import CSV exports created by [ESTOS MetaDirectory](https://www.estos.com/products/metadirectory) into one or more telephone books of an [Innovaphone PBX](https://www.innovaphone.com/en/ip-telephony/innovaphone-pbx.html).

## Cause of origin
The reason for the origin of this project is that a customer with several physically separate locations and independent databases (based on ODBC (MSSQL), Exchange, ...), uses a cloud-based telephone system (for all locations) from the Innovaphone brand and likes to indentify callers based on the existing data (customer data, contact data in MS Outlook) in order to help the caller as quickly as possible. Data such as the customer's name and customer number are thus displayed within the MyApps platform of the Innovaphone PBX and thus allow quick access to the customer account without the customer first having to be asked for his customer number, for example.

As an IT service provider, we were faced with the problem and had to collect the data from the individual data sources in a form that the Innovaphone could process and get it into the cloud PBX.

Since the ESTOS MetaDirectory maps our and many other [data sources](https://www.estos.com/produkte/metadirectory#c17904) and also already offers and also carries out a normalization of the telephone numbers for Innovaphone, we have decided on a combination of a purchased product and an in-house development.

The in-house development consisting of the conversion of the exports from the MetaDirectory into the format of the Innovaphone PBX, including the upload process, is part of this repository.

## Build
This project is currently not available for direct installation, so a build process must be carried out in advance.
A Visual Studio installation and a internet connection for cloning the repository and resolving the NuGet dependencies is therefore a prerequisite.

- Clone the repository via Visual Studio
- Open the solution file `ESTOSMetadirectory2InnovaphoneContacts.sln`
- Create a build using the keyboard shortcut `CTRL + B`

On success you can find the debug build in the `\bin\debug\` directory within the project.

## Installation
As already described in the Build section, there is currently no directly installable version of the project.
The creation of a build is therefore a prerequisite.

- Copy the `debug` folder to the destination machine with installed .Net Framework 4.8
- Open command prompt as administrator on destination machine
- Enter `sc.exe create ESTOSMetadirectory2InnovaphoneContacts binpath="{DESTINATION_FOLDER}\ESTOSMetadirectory2InnovaphoneContacts.exe"`

> **_NOTE:_**  The service requires an existing executable of `curl` in `%SystemRoot%\system32` with `digest` auth support.

## Configuration
### Innovaphone PBX
- Ensure that the `https://example.com/yourPBXDomain/contacts/posts/` - endpoint of the Contacts App is available for the machine running the service
- Open `PBX Manager` App
  - Open the configuration window for `AP Contacts`
  - Fill out the fields for `User (HTTP Post)` and `Password (HTTP Post)`
### Windows
- Open the `Credential Manager`
- Click on the button `Windows Credentials`
- Click on `Add generic credential`
- Fill out the fields
  - Internet or network address: `ESTOSMetadirectory2InnovaphoneContacts`
  - Username: `%1|%2[|%3]`
    - `%1` is the domain where your Innovaphone PBX is reachable (e.g. `https://example.com`)
    - `%2` is the value entered in the `User (HTTP Post)` field (e.g . `contactsupload`)
    - `%3` is an optional parameter if the innovaphone domain does not equal the domain
  - Password: the value entered in the `Password (HTTP Post)` field (e.g. `mysecretpassword`)
  - Start the `ESTOSMetadirectory2InnovaphoneContacts` service (e.g. with `sc start ESTOSMetadirectory2InnovaphoneContacts` in command line prompt)
 
  During the first start of the service the directory `%systemroot%\ESTOSMetadirectory2InnovaphoneContacts` gets automatically created.
  
  Configure the (CSV) Export Replicator of ESTOS MetaDirectory to place the output file into this directory.
  The service watches this folder every five (5) minutes for processable files.

  > **_NOTE:_**  The name of the telephone book equals the name of the file placed in the service directory.
  
  Errors and information related to the service are being piped to the Windows Event Viewer.

  #### Start parameters
  The service supports the following start parameters:

  | Parameter  | Possible values | Default value | Description |
  | ------------- | :-------------: | :-------------: | ------------- |
  | `/allowInsecureConnection`  | `true`<br />`false` | `false` | Allows or disallows insecure certicates at the Innovaphone PBX endpoint. |

  Any parameter requires the following format in the _Start parameters_ field in the service configuration window:
  `/PARAMETER=VALUE`

  If you use multiple start parameters you need to seperate each with a space.

