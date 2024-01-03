import axios, { AxiosStatic } from 'axios'
import { resourceService } from './resourceService'
import { translateService } from './translateService'
import { languageService } from './languageService'

const services = (axios: AxiosStatic) => ({
    resource: resourceService(axios),
    translate: translateService(axios),
    language: languageService(axios)
})

export default services(axios)
