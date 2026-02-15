import axios, { AxiosStatic } from 'axios'
import { Services } from '@/ts'
import { translationRequestService } from '@/services/translationRequestService'
import { authService } from './authService'
import { subtitleService } from './subtitleService'
import { translateService } from './translateService'
import { settingService } from './settingService'
import { mediaService } from './mediaService'
import { versionService } from './versionService'
import { scheduleService } from '@/services/scheduleService'
import { mappingService } from '@/services/mappingService'
import { directoryService } from '@/services/directoryService'
import { statisticsService } from '@/services/statisticsService'
import { telemetryService } from '@/services/telemetryService'
import { logsService } from '@/services/logsService'
import { requestTemplateService } from '@/services/requestTemplateService'

axios.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response?.status === 403 && error.response?.data?.onboardingRequired) {
            window.location.href = '/auth/onboarding'
            return
        }
        if (error.response?.status === 401) {
            const currentPath = window.location.pathname
            if (
                !currentPath.startsWith('/auth/login') &&
                !currentPath.startsWith('/auth/onboarding')
            ) {
                window.location.href = '/auth/login'
                return
            }
        }
        return Promise.reject(error)
    }
)

const services = (axios: AxiosStatic): Services => ({
    auth: authService(axios),
    setting: settingService(axios),
    subtitle: subtitleService(axios),
    translate: translateService(axios),
    translationRequest: translationRequestService(axios),
    version: versionService(axios),
    media: mediaService(axios),
    schedule: scheduleService(axios),
    mapping: mappingService(axios),
    directory: directoryService(axios),
    statistics: statisticsService(axios),
    logs: logsService(),
    telemetry: telemetryService(axios),
    requestTemplate: requestTemplateService(axios)
})

export default services(axios)
