import axios, { AxiosStatic } from 'axios'
import { Services } from '@/ts'
import { subtitleService } from './subtitleService'
import { translateService } from './translateService'
import { settingService } from './settingService'
import { mediaService } from './mediaService'
import { versionService } from './versionService'

const services = (axios: AxiosStatic): Services => ({
    setting: settingService(axios),
    subtitle: subtitleService(axios),
    translate: translateService(axios),
    version: versionService(axios),
    media: mediaService(axios)
})

export default services(axios)
