import axios, { AxiosInstance } from 'axios'
import resourceService from './resourceService'
import translateService from './translateService'
import languageService from './languageService'

const services = (axios: AxiosInstance) => ({
    resource: resourceService(axios),
    translate: translateService(axios),
    language: languageService(axios)
})

export default services(axios)
