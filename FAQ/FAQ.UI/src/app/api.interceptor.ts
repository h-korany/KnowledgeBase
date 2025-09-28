import { HttpEvent, HttpInterceptorFn, HttpErrorResponse, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export const ApiInterceptor: HttpInterceptorFn = (request: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> => {
  // Check for a token
  const token = localStorage.getItem('token');
  let modifiedRequest = request;

  if (token) {
    // If token exists, clone the request and add the Authorization header
    modifiedRequest = request.clone({
      headers: request.headers.set('Authorization', `Bearer ${token}`)
    });
console.log("interceptor")
  }

  // Handle the request and pipe the error handling
  return next(modifiedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMsg = '';
      if (error.error instanceof ErrorEvent) {
        // A client-side or network error occurred.
        errorMsg = `Client Error: ${error.error.message}`;
      } else {
        // The backend returned an unsuccessful response code.
        errorMsg = `Server Error: ${error.status} - ${error.message}`;
      }

      console.error('API Error:', errorMsg);
      // Return an observable with a user-facing error message
      return throwError(() => new Error(errorMsg));
    })
  );
};