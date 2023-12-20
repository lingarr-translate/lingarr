import fs from 'fs'
import fsp from 'fs/promises'
import subtitleService from './subtitle.service'
import { Directory } from '../ts/directory'
import path from 'path'

class ResourceService {
    /**
     * Asynchronously retrieves a directory listing or subtitle information for the specified path.
     *
     * @param {string} [resource=''] - The path to the directory or subtitle file. Defaults to an empty string, representing the default directory.
     * @returns {Promise<Directory>} A promise resolving to a Directory object representing the directory listing or a file containing subtitle information.
     * @throws {Error} Throws an error if any file operation fails or if the provided path does not exist.
     */
    async getDirectoryList(resource = ''): Promise<Directory> {
        const currentDirectoryOrFileName = resource.split('/').pop()?.length
            ? resource.split('/').pop()
            : ''
        const fullPath = path.resolve(__dirname, `../../media/${resource}`)
        let parentDirectory = ''

        // get parent dir
        if (resource.includes('/')) {
            parentDirectory = resource.split('/').slice(0, -1).join('/') || '/'
        }
        // is it a srt file?
        if (fs.lstatSync(fullPath).isFile() == true) {
            // read the file
            const file = (await fsp.readFile(fullPath, { encoding: 'utf8' })) ?? ''
            const result = await subtitleService.toArray(file)
            return {
                currentDirectory: currentDirectoryOrFileName,
                parentDirectory: parentDirectory,
                list: [],
                subtitle: result
            }
        }
        // is it not a dir or file
        if (fs.lstatSync(fullPath).isDirectory() == false) {
            console.log('empty directory or wrong file')
            return {
                currentDirectory: currentDirectoryOrFileName,
                parentDirectory: parentDirectory,
                list: []
            }
        }
        const dir = fs.opendirSync(fullPath)
        let entity
        const listing: Directory = {
            currentDirectory: currentDirectoryOrFileName,
            parentDirectory: parentDirectory,
            list: []
        }
        // read the directory and collect the contents
        while ((entity = dir.readSync()) !== null) {
            if (entity.isFile()) {
                const fileExt = entity.name.slice(((entity.name.lastIndexOf('.') - 1) >>> 0) + 2)
                if (fileExt === 'srt') {
                    listing.list.push({ type: fileExt, name: entity.name })
                }
            } else if (entity.isDirectory()) {
                listing.list.push({ type: 'directory', name: entity.name })
            }
        }
        dir.closeSync()
        listing.list.sort((a, b) => {
            // First, sort by type
            if (a.type < b.type) {
                return -1 // Directories come before files
            }
            if (a.type > b.type) {
                return 1 // Files come after directories
            }

            // If types are the same, then sort by name
            if (a.name.toUpperCase() < b.name.toUpperCase()) {
                return -1
            }
            if (a.name.toUpperCase() > b.name.toUpperCase()) {
                return 1
            }

            return 0 // Names are equal
        })
        return listing
    }

    /**
     * Asynchronously saves translated subtitles to a file in the specified target language.
     *
     * @param {string} resource - The path to the original subtitle file.
     * @param {string} targetLanguage - The target language code for the translated subtitles.
     * @param {string} subtitles - The translated subtitles to be written to the file.
     * @returns {Promise<Array<string>>} A promise resolving to an array with a success message upon successful creation, or an empty array if the file is not valid.
     * @throws {Error} Throws an error if any file operation fails or if the provided path does not point to a valid file.
     */
    async writeSubtitle(resource, targetLanguage, subtitles) {
        console.log('Writing subtitles...')
        try {
            const fullPath = path.resolve(__dirname, `../../media/${resource}`)
            if (fs.lstatSync(fullPath).isFile() == true) {
                const filePath = fullPath.replace(
                    /(\.([a-zA-Z]{2,3}))?\.srt$/,
                    `.${targetLanguage}.srt`
                )
                fs.writeFileSync(filePath, subtitles)
                return ['success']
            }
        } catch (error) {
            console.error(error)
        }
    }

    /**
     * Asynchronously reads subtitles from a file.
     *
     * @param {string} resource - The path to the subtitle file.
     * @returns {Promise<string>} A promise resolving to the content of the subtitle file as a string.
     * @throws {Error} Throws an error if any file operation fails or if the provided path does not point to a valid file.
     */
    async readSubtitle(resource) {
        const fullPath = path.resolve(__dirname, `../../media/${resource}`)
        if (fs.lstatSync(fullPath).isFile() == true) {
            // read the file
            return (await fsp.readFile(fullPath, { encoding: 'utf8' })) ?? ''
        }
    }
}

export default new ResourceService()
