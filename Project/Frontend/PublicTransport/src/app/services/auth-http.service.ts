import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { BaseHttpService } from './base-http.service';
import { User } from '../models/user.model';
import { Registration } from '../models/registration.model';
import { NgForm } from '@angular/forms';

@Injectable()
export class AuthHttpService{
    base_url = "http://localhost:52296/";
    loginUrl = "oauth/token"

    constructor(private http: HttpClient){
    }

    logIn(user: User, callback: any){
        let data = `username=${user.username}&password=${user.password}&grant_type=password`;
        let httpOptions = {
            headers: {
                "Content-type": "application/x-www-form-urlencoded"
            }
        }

        this.http.post<any>(this.base_url + this.loginUrl, data, httpOptions)
        .subscribe(data => {
              
          let jwt = data.access_token;

          let jwtData = jwt.split('.')[1]
          let decodedJwtJsonData = window.atob(jwtData)
          let decodedJwtData = JSON.parse(decodedJwtJsonData)

          let role = decodedJwtData.role

          console.log('jwtData: ' + jwtData)
          console.log('decodedJwtJsonData: ' + decodedJwtJsonData)
          console.log('decodedJwtData: ' + JSON.stringify(decodedJwtData))
          console.log('Role ' + role)

          localStorage.setItem('jwt', jwt)
          localStorage.setItem('role', role);
          callback(true);
          
        },
        err => {
            callback(false, err.status);
        });
    }
}