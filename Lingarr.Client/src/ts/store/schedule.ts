export interface IUseScheduleStore {
    runningJobs: IRunningJob[]
}

export interface IRunningJob {
    path: string
    language: string
    fileName: string
    jobId: string
}
