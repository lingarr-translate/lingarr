import axios, { AxiosStatic } from 'axios'
import { Services } from '@/ts'
import { subtitleService } from './subtitleService'
import { translateService } from './translateService'
import { settingService } from './settingService'
import { mediaService } from './mediaService'

const services = (axios: AxiosStatic): Services => ({
    setting: settingService(axios),
    subtitle: subtitleService(axios),
    translate: translateService(axios),
    media: mediaService(axios)
})

export default services(axios)
