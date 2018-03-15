import React, { Component } from 'react';
import { Switch, Route } from 'react-router-dom'

import PlagiarismsPage from './instructor/PlagiarismsPage'

class CourseController extends Component {
    render() {
        return (    
            <div>
                <div>{this.props.match.url}</div>
                <Switch>
                    <Route exact path="/:courseId" component={PlagiarismsPage} />
                </Switch>
            </div>
        )
    }
}

export default CourseController