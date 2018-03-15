import React, { Component } from 'react';
import { Switch, Route } from 'react-router-dom'

import IndexPage from './IndexPage'

class HomeController extends Component {
    render() {
        return (
            <Switch>
                <Route exact path="/" component={IndexPage} />
            </Switch>
        )
    }
}

export default HomeController