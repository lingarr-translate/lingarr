export interface IUseScheduleStore {
    recurringJobs: IRecurringJob[]
}

export interface IRunningJob {
    path: string
    language: string
    fileName: string
    jobId: string
}

export interface IRecurringJob {
    id: string
    cron: string
    queue: string
    jobMethod: string
    nextExecution: Date | null
    lastJobId: string | null
    lastJobState: string | null
    lastExecution: Date | null
    createdAt: Date | null
    timeZoneId: string | null
    currentState: string
    isCurrentlyRunning: boolean
    currentJobId: string | null
}
