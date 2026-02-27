import { IEncryptedSettings, ISettings } from '@/ts'

export interface IUseSettingStore {
    settings: ISettings
    encrypted_settings: IEncryptedSettings
}
