import React, { Component } from 'react'
import { Navbar, Nav, NavDropdown } from 'react-bootstrap'
import { Link } from 'react-router-dom'

import './Header.css'

class NavbarIcons extends Component {
    render() {
        return (
            <button type="button" className="navbar-toggle hide-side-bar-button" data-toggle="collapse" data-target={this.props.target}>
                <span className="icon-bar"/>
                <span className="icon-bar"/>
                <span className="icon-bar"/>
            </button>
        )
    }
}
 
class Header extends Component {
    render() {
        return (
            <Navbar fixedTop inverse>
                <NavbarIcons target=".side-bar"/>
                <div className="container">
                    <Navbar.Header>
                        <NavbarIcons target=".greeting-collapse-class"/>
                        <Navbar.Brand>
                            <Link to="/">Ulearn</Link>                            
                        </Navbar.Brand>
                        <Nav className="notifications__mobile-nav pull-right visible-xs">
                            <NavDropdown title="Dropdown">
                                <a href="Feed/NotificationsTopbarPartial?isMobile=true" />
                            </NavDropdown>
                        </Nav>
                    </Navbar.Header>
                    <ul className="notifications__mobile-dropdown notifications__dropdown navbar-collapse collapse visible-xs">

                    </ul>
                    <div className="greeting-collapse-class navbar-collapse collapse">
                        <ul className="top-navigation nav navbar-nav">
                        </ul>
                    </div>
                </div>
            </Navbar>            
        )
    }
}

export default Header;