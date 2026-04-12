import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LoggerService {

  logEnabled : boolean
  
  constructor() 
  { 
    this.logEnabled = environment.logEnabled
  }

  log(msg : any)
  {
    if(this.logEnabled)
    {
      console.log(msg)
    }
  }

  error(msg : any)
  {
    if(this.logEnabled)
    {
      console.error(msg)
    }
  }

}
