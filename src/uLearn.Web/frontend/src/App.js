import React, { Component } from 'react';
import logo from './logo.svg';
import './App.css';
import { Switch, Route } from 'react-router-dom';

import HomeController from './pages/home'
import CourseController from "./pages/course";
import Header from './components/common/Header';

class UlearnApp extends Component {
  render() {
    return (
      <div className="App">
        <Header />
        <Switch>
            <Route exact path="/" component={HomeController} />
            <Route path="/course/" component={CourseController} />
        </Switch>
      </div>
    );
  }
}

export default UlearnApp;
