import { Node } from 'subtitle'

export type Listing = {
    type: string
    name: string
}

export type Directory = {
    currentDirectory: string | undefined
    parentDirectory: string | undefined
    subtitle?: Node[]
    list: Listing[]
}
