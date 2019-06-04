import { Component, OnInit } from '@angular/core';
import { AuthHttpService } from 'src/app/services/auth-http.service';
import { Router } from '@angular/router';
import { User } from 'src/app/models/user.model';
import { NgForm } from '@angular/forms';
import { LoginToNavbarService } from 'src/app/services/login-to-navbar.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {

  isLoggedIn = false;
  isBadLoginParams: boolean;
  isOtherError: boolean;

  constructor(
    private http: AuthHttpService, 
    private router: Router,
    private loginToNavbar: LoginToNavbarService) { }

  ngOnInit() {
  }

  login(user: User, form: NgForm){
    this.http.logIn(user, (isLoggedIn, errorStatus) => {
      if(isLoggedIn){
        this.isLoggedIn = isLoggedIn;
        this.loginToNavbar.login();
        this.router.navigate(['/home']);
      }
      else{
        if(errorStatus === 400){
          this.isBadLoginParams = true;
        }
        else{
          this.isOtherError = true;
        }
      }
    });
    form.reset();
  }
}