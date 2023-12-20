import express, { Application } from 'express'
import Server from './src'
import 'dotenv/config'

const app: Application = express()
new Server(app)

const PORT: number = process.env.PORT ? parseInt(process.env.PORT, 10) : 9876
app.listen(PORT, () => {
    console.log(`Server is listening on port ${PORT}.`)
}).on('error', (err: any) => {
    if (err.code === 'EADDRINUSE') {
        console.log('Error: address already in use')
    } else {
        console.log(err)
    }
})
