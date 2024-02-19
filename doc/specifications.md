# Eltrovo Software Requirement Specifications

## Introduction

Eltrovo is a software tool for archivists that will enable discovery, navigation, and description of digital collections. Archivists have many workflows at their disposal for processing digital collections depending on the nature of the archives and the collection. Eltrovo does not address the processing of regulated accumulations in an institutional context. Such accumulations are governed by institutional policies (Roy et al., 2022) and the contents are therefore comparatively well understood. Instead Eltrovo addresses the problem of unregulated digital collections, either institutional or personal, that arrive with little context. These collections may take the form of a hard drive, CDs, DVDs, or other digital media into which the archivist has no foresight. Without the benefit of institutional policies or the cooperation of the records' creator the archivist is faced with the problem of discovering the contents of those media, navigating them, and describing them. These can be excessively time-consuming and manual tasks even with the help of extant tools such as BitCurator, leading archivists to employ a minimal level of processing when the size of the collection makes it too complex for the archives' resources (Waugh et al., 2016). Eltrovo will reduce the amount of time required for an archivist to survey such collections and may thus allow the archives to process them at a higher level.

The Open Archival Information System (OAIS) models the workflow by which an archives may receive a donation, process it, and provide access. After the archives receives the digital assets and packages them in a submission information package (SIP), the SIP progresses to the ingest stage where it is processed into an archival information package (AIP) consisting of archival file formats and descriptive metadata (*Reference Model for an Open Archival Information System (OAIS)*, 2012). Eltrovo fits into this workflow between the creation of the SIP and the AIP when the digital media have already been imaged but before archival masters are created. At this stage the archivist examines the contents of the images so that they can make the most appropriate decisions about how to process them, and it is at this point in the workflow when Eltrovo will help the archivist understand the contents of the SIP.

## Functional Requirements

Upon starting the program the user will be able to select one or more file system images for analysis. Eltrovo will be able to handle file systems cloned with block-by-block copying tools such as `dd` and Expert Witness Format (EWF) forensic images. Eltrovo will mount these imaged file systems in read-only mode, ensuring that the contents and metadata of the image are not altered. In order to mount the file systems Eltrovo will need to be able to read multiple kinds of file systems, including historical file systems no longer in current use. These may include EXT, APFS, HFS(+), FAT, NTFS, and ISO 9660. The ability to read these file systems, however, may be a function of the operating system and thus fall outside the scope of Eltrovo itself.

After mounting the file systems Eltrovo will iterate through each file on the mounted file systems, log the file's name, path, and MIME type to an internal database. The internal database will thus contain an internal map of the file system. Eltrovo will compare the contents of each file with the contents of each other file (on the same file system and on others) to identify the following:

- identical files whose binary contents are the same,
- identical files whose binary contents are not the same (e.g. an image that has been saved in two different file formats),
- edited versions or drafts of the same work, and
- files whose contents are included in another file (e.g. an image embedded in a PDF, a Word document attached to an email).

Eltrovo must be able to read the following common text-based file formats:

- TXT,
- DOCX,
- DOC,
- RTF,
- PDF, and
- ODT.

Eltrovo must be able to read the following common image-based file formats:

- JPEG,
- PNG,
- TIFF,
- GIF, and
- BMP.

Eltrovo must be able to read the following common video-based file formats:

- MPEG-4,
- MPEG-1,
- MPEG-2,
- RealMedia,
- Windows Media Video,
- AVI, and
- QuickTime.

After finding the relationships between files, Eltrovo will classify the relationships as identical (for bitwise copies), scaled (for identical resources that have been rescaled or re-encoded), included (for content that is reproduced entirely in other files), or similar. More than one classification is possible to describe the relationship between two files.

Eltrovo will save the metadata about similar files to its internal database. It will allow the user to review its findings, make their own annotations, and alter annotations created by the software. The user will be able to save the results to a file. They will also be able to exit Eltrovo and open the file again at a later date to resume their work. Upon exiting, Eltrovo will unmount the file system images, and upon opening a saved file Eltrovo will try to re-mount the images. Once the metadata have been created, however, the presence of the original file system images will no longer be required. Eltrovo will be able to access and edit its database even without the presence of the images, although the ability to examine that image again will not be possible unless the user restores access to the image.

Additional images may be added at a later date. This may not require re-examining previous file system images to determine some kinds of relationships, but may require re-examining previous file system images for other relationships.

## External Interface Requirements

Eltrovo will employ a user interface that allows the user to visualize Eltrovo's internal map of the file systems. The relationships between files will be annotated with the software's best guess as to their nature and these annotations will be made visible to the user through a combination of colors and labels. If they desire the user will be able to drill down into a relationship to find out more detailed information than Eltrovo can show in the broader visualization.

The interface will allow the archivist to navigate the relationships between files and make manual interventions to correct the automatic annotations of the software or to create their own annotations. The interface will allow the archivist to preview the contents of the files in an internal viewer, open the path using the operating system's file browser, or open the files in an external viewer for manual inspection. The interface will provide affordances to allow the user to see the diff between files or to compare two files side-by-side.

Eltrovo will allow its internal database to be exported in a standard file format such as XML or Turtle. The structure of Eltrovo's data will be informed by and compatible with the Records in Contexts-Conceptual Model (RiC-CM), a next-generation conceptual model for the description of archival records. This will allow a degree of interoperability with other software, though some transformation may be required depending on the nature of the other software.

Eltrovo will not require an Internet connection. It will not make any requests to remote computer systems.

## Non-Functional Requirements

The availability of Eltrovo is a key value and it is important that Eltrovo be usable on the operating system of the user's choice. It must therefore be possible to compile Eltrovo for Windows, macOS, and Linux with no major differences in functionality between different platforms.

Eltrovo must have an interface that is intuitive and easy to use without requiring knowledge of the command line or extensive technical training. Thorough documentation must, however, be made available and maintained. This documentation must include how to compile Eltrovo, instructions for use, a list of technical features, descriptions of how the software functions, any platform-based limitations, and crosswalks to convert Eltrovo's data for use with other platforms.

Eltrovo should employ RiC-CM to model and store information about files and their interrelations. Eltrovo's implementation of RiC-CM must remain up to date with the latest specifications. Eltrovo should balance consideration of the conceptual model with the goal of being backwards compatible with data produced by a previous version of the software. Eltrovo must provide upgrade paths for users with older data.

Consideration and respect for the user's time are high priorities for this project. Eltrovo should operate as efficiently as possible in order to process large volumes of data in a timely manner. The user's time, RAM, and CPU cycles are to be conserved as much as possible.

## References

*Reference model for an open archival information system (OAIS)* (tech. rep.). (2012). The Consultative Committee for Space Data Systems.

Roy, B. K., Biswas, S. C., & Mukhopadhyay, P. (2022). Collection development and organization in institutional digital repositories: From policy to practice. *International Journal of Information Science and Management, 20*(1), 15–39. https://doi.org/20.1001.1.20088302.2022.20.1.2.5

Waugh, D., Roke, E. R., & Farr, E. (2016). Flexible processing and diverse collections: A tiered approach to delivering born digital archives. *Archives and Records, 37*(1), 3–19. https://doi.org/10.1080/23257962.2016.1139493

